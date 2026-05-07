using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;
using trinova_erp_backend.Config;
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
    }
}