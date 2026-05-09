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
        Task<int> InsertQuotationHeader(
            QuotationHeader header,
            SqlConnection conn,
            SqlTransaction transaction
        );

        Task<bool> InsertQuotationDetail(
            QuotationDetail detail,
            SqlConnection conn,
            SqlTransaction transaction
        );

        Task<List<QuotationHeaderResponse>> GetAllQuotation();
    }

    public class SalesQuotationRepo : ISalesQuotationRepo
    {
        private readonly string _connectionString;

        public SalesQuotationRepo(IOptionsSnapshot<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer
                ?? throw new InvalidOperationException("Database connection string is not configured.");
        }

        public async Task<int> InsertQuotationHeader(
            QuotationHeader header,
            SqlConnection conn,
            SqlTransaction transaction
        )
        {
            string query = @"
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
                    discount_total
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
                    @DiscountTotal
                );

                SELECT CAST(SCOPE_IDENTITY() as int);
            ";

            int quotationId = await conn.ExecuteScalarAsync<int>(
                query,
                new
                {
                    header.CustomerId,
                    header.QuotationNumber,
                    header.QuotationDate,
                    header.Address,
                    header.Notes,
                    header.IsTaxable,
                    header.IsTaxIncluded,
                    header.Subtotal,
                    header.DiscountTotal
                },
                transaction
            );

            return quotationId;
        }

        public async Task<bool> InsertQuotationDetail(
            QuotationDetail detail,
            SqlConnection conn,
            SqlTransaction transaction
        )
        {
            string query = @"
                INSERT INTO quotation_detail
                (
                    quotation_id,
                    product_id,
                    quantity,
                    uom_id,
                    price,
                    discount_percent,
                    discount_amount
                )
                VALUES
                (
                    @QuotationId,
                    @ProductId,
                    @Quantity,
                    @UomId,
                    @Price,
                    @DiscountPercent,
                    @DiscountAmount
                );
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
                    detail.DiscountPercent,
                    detail.DiscountAmount
                },
                transaction
            );

            return result > 0;
        }

        public async Task<List<QuotationHeaderResponse>> GetAllQuotation()
        {
            string query = @"SELECT 
                            sq.quotation_id AS QuotationId,
                            sq.quotation_number AS QuotationNumber,
                            sq.quotation_date As QuotationDate,
                            mc.customer_name As CustomerName,
                            sq.notes As Notes,
                            sq.subtotal As SubTotal
                            FROM  sales_quotation sq 
                            JOIN master_customer mc on sq.customer_id  = mc.customer_id ";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var result = await connection.QueryAsync<QuotationHeaderResponse>(query);

                return result.ToList();
                
            }
        }
    }
}