using System.Net.Http.Headers;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.DTO;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public interface ISupplierRiskUsecase
    {
        /// <summary>
        /// Aggregates live ERP metrics for a single supplier, maps them to the
        /// FastAPI feature schema, and calls POST /predict/supplier-risk.
        /// </summary>
        Task<SupplierRiskPredictResponse> PredictSupplierRisk(int supplierId);

        /// <summary>
        /// Aggregates live ERP data for ALL active suppliers, maps to FastAPI
        /// training rows, and calls POST /train/from-rows to retrain the model.
        /// </summary>
        Task<SupplierRiskTrainResponse> TrainFromErpData(bool appendToExisting = true);

        /// <summary>
        /// Forwards a raw CSV file (multipart) to POST /train/from-csv-upload.
        /// The CSV must contain the columns:
        /// supplier_price, lead_time_days, claim_rate, on_time_rate, order_frequency, late_delivery
        /// </summary>
        Task<SupplierRiskTrainResponse> TrainFromCsvUpload(Stream csvStream, string fileName);

        /// <summary>
        /// Triggers retraining using the bundled CSV on the FastAPI server's disk.
        /// Optionally pass a custom server-side path.
        /// </summary>
        Task<SupplierRiskTrainResponse> TrainFromServerCsv(string? csvPath = null);
    }

    public class SupplierRiskUsecase : ISupplierRiskUsecase
    {
        private readonly IHttpClientFactory    _httpClientFactory;
        private readonly DatabaseConnection    _dbConfig;

        // snake_case serializer to match FastAPI's field names
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNamingPolicy        = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        public SupplierRiskUsecase(
            IHttpClientFactory           httpClientFactory,
            IOptions<DatabaseConnection> dbConfig
        )
        {
            _httpClientFactory = httpClientFactory;
            _dbConfig          = dbConfig.Value;
        }

        // ── Predict ───────────────────────────────────────────────────────────

        public async Task<SupplierRiskPredictResponse> PredictSupplierRisk(int supplierId)
        {
            var agg = await AggregateSupplier(supplierId)
                      ?? throw new KeyNotFoundException(
                             $"Supplier {supplierId} not found or has no purchase history.");

            // Map ERP aggregated fields → FastAPI SupplierInput fields
            var payload = new SupplierRiskPredictRequest
            {
                supplier_id      = agg.supplier_id,
                supplier_price   = agg.total_po_value,        // total PO value as price proxy
                lead_time_days   = Math.Max(1, (int)Math.Round(agg.avg_delivery_days)),
                claim_rate       = agg.claim_rate,
                on_time_rate     = agg.on_time_rate,
                order_frequency  = Math.Max(1, agg.total_orders)
            };

            var client   = _httpClientFactory.CreateClient("XGBoost");
            var response = await client.PostAsJsonAsync("/predict/supplier-risk", payload, _jsonOpts);

            await EnsureSuccessAsync(response, "predict/supplier-risk");

            return await response.Content.ReadFromJsonAsync<SupplierRiskPredictResponse>(_jsonOpts)
                   ?? throw new InvalidOperationException("FastAPI returned an empty predict response.");
        }

        // ── Train from live ERP data ──────────────────────────────────────────

        public async Task<SupplierRiskTrainResponse> TrainFromErpData(bool appendToExisting = true)
        {
            var rows = await AggregateAllSuppliers();

            if (rows.Count == 0)
                throw new InvalidOperationException(
                    "No supplier data found in the ERP database to train on.");

            // Map ERP aggregated fields → FastAPI REQUIRED_COLUMNS
            // Label: high risk when claim_rate > 0.1 OR on_time_rate < 0.8
            var trainingRows = rows.Select(r => new SupplierRiskTrainingRow
            {
                supplier_id     = r.supplier_id,
                supplier_price  = r.total_po_value,
                lead_time_days  = Math.Max(1, (int)Math.Round(r.avg_delivery_days)),
                claim_rate      = r.claim_rate,
                on_time_rate    = r.on_time_rate,
                order_frequency = Math.Max(1, r.total_orders),
                late_delivery   = (r.claim_rate > 0.1 || r.on_time_rate < 0.8) ? 1 : 0
            }).ToList();

            var requestBody = new SupplierRiskTrainFromRowsRequest
            {
                rows               = trainingRows,
                append_to_existing = appendToExisting
            };

            var client   = _httpClientFactory.CreateClient("XGBoost");
            var response = await client.PostAsJsonAsync("/train/from-rows", requestBody, _jsonOpts);

            await EnsureSuccessAsync(response, "train/from-rows");

            return await response.Content.ReadFromJsonAsync<SupplierRiskTrainResponse>(_jsonOpts)
                   ?? throw new InvalidOperationException("FastAPI returned an empty train response.");
        }

        // ── Train from CSV upload ─────────────────────────────────────────────

        public async Task<SupplierRiskTrainResponse> TrainFromCsvUpload(Stream csvStream, string fileName)
        {
            var client  = _httpClientFactory.CreateClient("XGBoost");
            var content = new MultipartFormDataContent();

            var fileContent = new StreamContent(csvStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            content.Add(fileContent, "file", fileName);

            var response = await client.PostAsync("/train/from-csv-upload", content);

            await EnsureSuccessAsync(response, "train/from-csv-upload");

            return await response.Content.ReadFromJsonAsync<SupplierRiskTrainResponse>(_jsonOpts)
                   ?? throw new InvalidOperationException("FastAPI returned an empty train response.");
        }

        // ── Train from server-side CSV ────────────────────────────────────────

        public async Task<SupplierRiskTrainResponse> TrainFromServerCsv(string? csvPath = null)
        {
            var client  = _httpClientFactory.CreateClient("XGBoost");
            var payload = csvPath is null
                ? new { csv_path = (string?)null }
                : new { csv_path = (string?)csvPath };

            var response = await client.PostAsJsonAsync("/train", payload, _jsonOpts);

            await EnsureSuccessAsync(response, "train");

            return await response.Content.ReadFromJsonAsync<SupplierRiskTrainResponse>(_jsonOpts)
                   ?? throw new InvalidOperationException("FastAPI returned an empty train response.");
        }

        // ── SQL aggregation helpers ───────────────────────────────────────────

        private async Task<SupplierRiskAggregated?> AggregateSupplier(int supplierId)
        {
            const string sql = @"
                SELECT
                    s.supplier_id,
                    s.supplier_name,
                    ISNULL(SUM(po.total_amount), 0)                                  AS total_po_value,
                    COUNT(DISTINCT po.purchase_order_id)                             AS total_orders,
                    CASE
                        WHEN COUNT(DISTINCT po.purchase_order_id) = 0 THEN 0.0
                        ELSE CAST(COUNT(DISTINCT pr.purchase_return_id) AS FLOAT)
                             / COUNT(DISTINCT po.purchase_order_id)
                    END                                                              AS claim_rate,
                    CASE
                        WHEN COUNT(DISTINCT gr.goods_receipt_id) = 0 THEN 1.0
                        ELSE CAST(
                            SUM(CASE WHEN gr.receipt_date <= po.expected_date THEN 1 ELSE 0 END)
                            AS FLOAT) / COUNT(DISTINCT gr.goods_receipt_id)
                    END                                                              AS on_time_rate,
                    ISNULL(AVG(CAST(DATEDIFF(day, po.order_date, gr.receipt_date) AS FLOAT)), 3)
                                                                                     AS avg_delivery_days
                FROM master_supplier         s
                LEFT JOIN purchase_order     po ON po.supplier_id         = s.supplier_id
                LEFT JOIN goods_receipt      gr ON gr.purchase_order_id   = po.purchase_order_id
                LEFT JOIN purchase_return    pr ON pr.goods_receipt_id    = gr.goods_receipt_id
                WHERE s.supplier_id = @SupplierId
                GROUP BY s.supplier_id, s.supplier_name";

            await using var conn = new SqlConnection(_dbConfig.SQLServer);
            return await conn.QuerySingleOrDefaultAsync<SupplierRiskAggregated>(
                sql, new { SupplierId = supplierId });
        }

        private async Task<List<SupplierRiskAggregated>> AggregateAllSuppliers()
        {
            const string sql = @"
                SELECT
                    s.supplier_id,
                    s.supplier_name,
                    ISNULL(SUM(po.total_amount), 0)                                  AS total_po_value,
                    COUNT(DISTINCT po.purchase_order_id)                             AS total_orders,
                    CASE
                        WHEN COUNT(DISTINCT po.purchase_order_id) = 0 THEN 0.0
                        ELSE CAST(COUNT(DISTINCT pr.purchase_return_id) AS FLOAT)
                             / COUNT(DISTINCT po.purchase_order_id)
                    END                                                              AS claim_rate,
                    CASE
                        WHEN COUNT(DISTINCT gr.goods_receipt_id) = 0 THEN 1.0
                        ELSE CAST(
                            SUM(CASE WHEN gr.receipt_date <= po.expected_date THEN 1 ELSE 0 END)
                            AS FLOAT) / COUNT(DISTINCT gr.goods_receipt_id)
                    END                                                              AS on_time_rate,
                    ISNULL(AVG(CAST(DATEDIFF(day, po.order_date, gr.receipt_date) AS FLOAT)), 3)
                                                                                     AS avg_delivery_days
                FROM master_supplier         s
                LEFT JOIN purchase_order     po ON po.supplier_id         = s.supplier_id
                LEFT JOIN goods_receipt      gr ON gr.purchase_order_id   = po.purchase_order_id
                LEFT JOIN purchase_return    pr ON pr.goods_receipt_id    = gr.goods_receipt_id
                WHERE s.status = 'Active'
                GROUP BY s.supplier_id, s.supplier_name
                HAVING COUNT(DISTINCT po.purchase_order_id) > 0";

            await using var conn = new SqlConnection(_dbConfig.SQLServer);
            var result = await conn.QueryAsync<SupplierRiskAggregated>(sql);
            return result.ToList();
        }

        // ── Shared error helper ───────────────────────────────────────────────

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string endpoint)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"FastAPI /{endpoint} returned {(int)response.StatusCode}: {body}");
            }
        }
    }
}
