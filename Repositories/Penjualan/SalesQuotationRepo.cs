using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.DTO;
using trinova_erp_backend.Models.Penjualan;

namespace trinova_erp_backend.Repositories.Penjualan
{
    public interface ISalesQuotationRepo
    {
        Task<List<QuotationHeaderDTO>> GetQuotationHeaders();
        Task<List<QuotationHeaderDTO>> GetQuotationHeaderById(int customerId);
        Task<List<QuotationDetailDTO>> GetQuotationDetailById(int quotationId);

        Task<QuotationHeaderDetailDTO?> GetQuotationHeaderDetailById(int quotationId);

        Task<int> UpsertQuotationHeader(
            QuotationHeader header,
            SqlConnection conn,
            SqlTransaction transaction
        );

        Task<bool> UpsertQuotationDetail(
            QuotationDetail detail,
            SqlConnection conn,
            SqlTransaction transaction
        );

        /// <summary>
        /// Moves a quotation from Draft to Sent once it has actually been emailed
        /// to the customer. Only transitions FROM Draft -- a quotation that has
        /// already moved on (Approved/Processed/Rejected/Cancelled) must not be
        /// regressed back to Sent just because it was re-sent by email.
        /// </summary>
        Task MarkAsSentAsync(int quotationId);
    }

    public class SalesQuotationRepo : ISalesQuotationRepo
    {
        private readonly string _connectionString;

        public SalesQuotationRepo(IOptionsSnapshot<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer
                ?? throw new InvalidOperationException("Database connection string is not configured.");
        }

        public async Task<List<QuotationHeaderDTO>> GetQuotationHeaders()
        {
            string query = @"SELECT 
                            sq.quotation_id    AS Id,
                            sq.quotation_number AS QuotationNumber, 
                            sq.quotation_date   AS QuotationDate,
                            mc.customer_name    AS CustomerName,
                            sq.notes            AS Notes,
                            sq.subtotal         AS Subtotal,
                            ISNULL(sq.status, 'Draft') AS Status
                            FROM sales_quotation sq JOIN master_customer mc  on sq.customer_id = mc.customer_id ";

            using var connection = new SqlConnection(_connectionString);
            var result = await connection.QueryAsync<QuotationHeaderDTO>( query );
            return result.ToList();
        }

        public async Task<List<QuotationHeaderDTO>> GetQuotationHeaderById(int customerId)
        {
            string query = @"SELECT 
                            sq.quotation_id    AS Id,
                            sq.quotation_number AS QuotationNumber, 
                            sq.quotation_date   AS QuotationDate,
                            mc.customer_name    AS CustomerName,
                            sq.notes            AS Notes,
                            sq.subtotal         AS Subtotal,
                            ISNULL(sq.status, 'Draft') AS Status
                            FROM sales_quotation sq JOIN master_customer mc  on sq.customer_id = mc.customer_id
                            WHERE mc.customer_id = @CustomerId";

            using var connection = new SqlConnection(_connectionString);
            var result = await connection.QueryAsync<QuotationHeaderDTO>(
                query,
                new
                {
                    CustomerId = customerId
                });

            return result.ToList();
        }


        // QUERY UNTUK PAGE DETAIL SALES QUOTATION / PENAWARAN PENJUALAN
        public async Task<QuotationHeaderDetailDTO?> GetQuotationHeaderDetailById(int quotationId)
        {
            string headerQuery = @"
            SELECT
                sq.quotation_id      AS Id,
                sq.quotation_number  AS QuotationNumber,
                sq.quotation_date    AS QuotationDate,
                mc.customer_name     AS CustomerName,
                mc.customer_id       AS CustomerId,
                mc.email             AS CustomerEmail,
                sq.address           AS Address,
                sq.notes             AS Notes,
                sq.subtotal          AS Subtotal,
                sq.discount_total    AS DiscountTotal,
                sq.is_taxable        AS IsTaxAble,
                sq.tax_total         AS TaxTotal,
                ISNULL(sq.status, 'Draft') AS Status
            FROM sales_quotation sq
            JOIN master_customer mc
                ON sq.customer_id = mc.customer_id
            WHERE sq.quotation_id = @QuotationId";

            string detailQuery = @"
            SELECT
                mp.product_id      AS ProductId,
                mp.product_code    AS ProductCode,
                mp.product_name    AS ProductName,
                qd.quantity        AS Quantity,
                mu.uom_id          AS UomId,
                mu.uom_code        AS UomCode,
                qd.price           AS Price,
                qd.discount_percent AS DiscountPercent
            FROM quotation_detail qd
            JOIN master_product mp
                ON qd.product_id = mp.product_id
            JOIN master_uom mu
                ON qd.uom_id = mu.uom_id
            WHERE qd.quotation_id = @QuotationId";

            using var connection = new SqlConnection(_connectionString);

            var header = await connection.QueryFirstOrDefaultAsync<QuotationHeaderDTO>(
                headerQuery,
                new { QuotationId = quotationId }
            );

            if (header == null)
                return null;

            var detail = await connection.QueryAsync<QuotationDetailDTO>(
                detailQuery,
                new { QuotationId = quotationId }
            );

            return new QuotationHeaderDetailDTO
            {
                Header = header,
                Detail = detail.ToList()
            };
        }

        // END QUERY

        public async Task<List<QuotationDetailDTO>> GetQuotationDetailById(int quotationId)
        {
            string query = @"SELECT
                            mp.product_id 	AS ProductId,
                            mp.product_code AS ProductCode,
                            mp.product_name AS ProductName,
                            qd.quantity     AS Quantity,
                            mu.uom_id       AS UomId,
                            mu.uom_code		AS UomCode,
                            qd.price 		AS Price,
                            qd.discount_percent AS DiscountPercent
                            FROM quotation_detail qd JOIN master_product mp on qd.product_id  = mp.product_id join
                            master_uom mu on qd.uom_id  = mu.uom_id
                            WHERE qd.quotation_id  = @QuotationId";
            using var connection = new SqlConnection(_connectionString);
            var result = await connection.QueryAsync<QuotationDetailDTO>(
                query,
                new
                {
                    QuotationId = quotationId
                }
                );
            return result.ToList();
        }



        public async Task<int> UpsertQuotationHeader(
    QuotationHeader header,
    SqlConnection conn,
    SqlTransaction transaction
)
        {
            try
            {
                string query = @"
            IF EXISTS (
                SELECT 1
                FROM sales_quotation
                WHERE quotation_id = @QuotationId
            )
            BEGIN
                UPDATE sales_quotation
                SET
                    customer_id = @CustomerId,
                    quotation_number = @QuotationNumber,
                    quotation_date = @QuotationDate,
                    address = @Address,
                    notes = @Notes,
                    is_taxable = @IsTaxable,
                    is_tax_included = @IsTaxIncluded,
                    subtotal = @Subtotal,
                    discount_total = @DiscountTotal,
                    tax_total = @TaxTotal
                WHERE quotation_id = @QuotationId;

                SELECT @QuotationId;
            END
            ELSE
            BEGIN
                INSERT INTO sales_quotation
                (
                    customer_id,
                    quotation_number,
                    quotation_date,
                    address,
                    notes,
                    is_taxable,
                    is_tax_included,
                    subtotal,
                    discount_total,
                    tax_total,
                    status
                )
                VALUES
                (
                    @CustomerId,
                    @QuotationNumber,
                    @QuotationDate,
                    @Address,
                    @Notes,
                    @IsTaxable,
                    @IsTaxIncluded,
                    @Subtotal,
                    @DiscountTotal,
                    @TaxTotal,
                    'Draft'
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
            END
        ";

                int quotationId = await conn.ExecuteScalarAsync<int>(
                    query,
                    new
                    {
                        header.QuotationId,
                        header.CustomerId,
                        header.QuotationNumber,
                        header.QuotationDate,
                        header.Address,
                        header.Notes,
                        header.IsTaxable,
                        header.IsTaxIncluded,
                        header.Subtotal,
                        header.DiscountTotal,
                        header.TaxTotal
                    },
                    transaction
                );

                return quotationId;
            }
            catch (Exception ex)
            {
                throw new Exception($"Gagal melakukan upsert quotation header: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpsertQuotationDetail(
    QuotationDetail detail,
    SqlConnection conn,
    SqlTransaction transaction
)
        {
            try
            {
                string query = @"
            IF EXISTS (
                SELECT 1
                FROM quotation_detail
                WHERE quotation_id = @QuotationId
                  AND product_id = @ProductId
            )
            BEGIN
                UPDATE quotation_detail
                SET
                    quantity = @Quantity,
                    uom_id = @UomId,
                    price = @Price,
                    discount_percent = @DiscountPercent
                WHERE quotation_id = @QuotationId
                  AND product_id = @ProductId;
            END
            ELSE
            BEGIN
                INSERT INTO quotation_detail
                (
                    quotation_id,
                    product_id,
                    quantity,
                    uom_id,
                    price,
                    discount_percent
                )
                VALUES
                (
                    @QuotationId,
                    @ProductId,
                    @Quantity,
                    @UomId,
                    @Price,
                    @DiscountPercent
                );
            END
        ";

                int result = await conn.ExecuteAsync(
                    query,
                    new
                    {
                        detail.QuotationId,
                        detail.ProductId,
                        detail.Quantity,
                        detail.UomId,
                        detail.Price,
                        detail.DiscountPercent
                    },
                    transaction
                );

                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Gagal melakukan upsert quotation detail: {ex.Message}", ex);
            }
        }

        public async Task MarkAsSentAsync(int quotationId)
        {
            const string query = @"
                UPDATE sales_quotation
                SET status = 'Sent'
                WHERE quotation_id = @QuotationId
                  AND status = 'Draft';";

            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(query, new { QuotationId = quotationId });
        }
    }
}
