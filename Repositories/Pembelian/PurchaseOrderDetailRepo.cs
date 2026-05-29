using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Repositories.Pembelian
{
    public interface IPurchaseOrderDetailRepo
    {
        Task<bool> InsertPurchaseOrderDetail(PurchaseOrderDetail model);

        Task<List<PurchaseOrderDetail>> GetAllPurchaseOrderDetail();

        Task<bool> UpdatePurchaseOrderDetail(PurchaseOrderDetail model);

        Task<bool> DeletePurchaseOrderDetail(int id);

        Task UpdatePurchaseOrderTotal(int purchaseOrderId);
    }

    public class PurchaseOrderDetailRepo : IPurchaseOrderDetailRepo
    {
        private readonly string _connectionString;

        public PurchaseOrderDetailRepo(IOptionsSnapshot<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer
                ?? throw new InvalidOperationException("Database connection string is not configured.");
        }

        // INSERT
        public async Task<bool> InsertPurchaseOrderDetail(PurchaseOrderDetail model)
        {
            const string query = @"
                INSERT INTO purchase_order_detail
                (
                    purchase_order_id,
                    product_id,
                    uom_id,
                    quantity,
                    price,
                    subtotal
                )
                VALUES
                (
                    @purchase_order_id,
                    @product_id,
                    @uom_id,
                    @quantity,
                    @price,
                    @subtotal
                )";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    command.Parameters.AddWithValue("@purchase_order_id", model.purchase_order_id);
                    command.Parameters.AddWithValue("@product_id", model.product_id);
                    command.Parameters.AddWithValue("@uom_id", model.uom_id);
                    command.Parameters.AddWithValue("@quantity", model.quantity);
                    command.Parameters.AddWithValue("@price", model.price ?? 0);
                    command.Parameters.AddWithValue("@subtotal", model.subtotal ?? 0);

                    int result = await command.ExecuteNonQueryAsync();

                    if (result > 0)
                    {
                        await UpdatePurchaseOrderTotal(model.purchase_order_id);
                    }

                    return result > 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // UPDATE
        public async Task<bool> UpdatePurchaseOrderDetail(PurchaseOrderDetail model)
        {
            const string query = @"
                UPDATE purchase_order_detail
                SET
                    product_id = @product_id,
                    uom_id = @uom_id,
                    quantity = @quantity,
                    price = @price,
                    subtotal = @subtotal
                WHERE purchase_order_detail_id = @purchase_order_detail_id";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    command.Parameters.AddWithValue("@purchase_order_detail_id", model.purchase_order_detail_id);
                    command.Parameters.AddWithValue("@product_id", model.product_id);
                    command.Parameters.AddWithValue("@uom_id", model.uom_id);
                    command.Parameters.AddWithValue("@quantity", model.quantity);
                    command.Parameters.AddWithValue("@price", model.price ?? 0);
                    command.Parameters.AddWithValue("@subtotal", model.subtotal ?? 0);

                    int result = await command.ExecuteNonQueryAsync();

                    if (result > 0)
                    {
                        await UpdatePurchaseOrderTotal(model.purchase_order_id);
                    }

                    return result > 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // DELETE
        public async Task<bool> DeletePurchaseOrderDetail(int id)
        {
            int purchaseOrderId = 0;

            const string getPurchaseOrderIdQuery = @"
                SELECT purchase_order_id
                FROM purchase_order_detail
                WHERE purchase_order_detail_id = @id";

            const string deleteQuery = @"
                DELETE FROM purchase_order_detail
                WHERE purchase_order_detail_id = @id";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // GET PURCHASE ORDER ID
                    using (
                        SqlCommand getCommand = new SqlCommand(
                            getPurchaseOrderIdQuery,
                            connection
                        )
                    )
                    {
                        getCommand.Parameters.AddWithValue("@id", id);

                        var resultId = await getCommand.ExecuteScalarAsync();

                        if (resultId != null)
                        {
                            purchaseOrderId = Convert.ToInt32(resultId);
                        }
                    }

                    // DELETE DETAIL
                    using (
                        SqlCommand deleteCommand = new SqlCommand(
                            deleteQuery,
                            connection
                        )
                    )
                    {
                        deleteCommand.Parameters.AddWithValue("@id", id);

                        int result = await deleteCommand.ExecuteNonQueryAsync();

                        if (result > 0)
                        {
                            await UpdatePurchaseOrderTotal(purchaseOrderId);
                        }

                        return result > 0;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // GET ALL
        public async Task<List<PurchaseOrderDetail>> GetAllPurchaseOrderDetail()
        {
            const string query = @"SELECT * FROM purchase_order_detail";

            var response = new List<PurchaseOrderDetail>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var detail = new PurchaseOrderDetail()
                            {
                                purchase_order_detail_id =
                                    reader.GetInt32(
                                        reader.GetOrdinal(
                                            "purchase_order_detail_id"
                                        )
                                    ),

                                purchase_order_id =
                                    reader.GetInt32(
                                        reader.GetOrdinal(
                                            "purchase_order_id"
                                        )
                                    ),

                                product_id =
                                    reader["product_id"] != DBNull.Value
                                        ? Convert.ToInt32(reader["product_id"])
                                        : 0,

                                uom_id =
                                    reader["uom_id"] != DBNull.Value
                                        ? Convert.ToInt32(reader["uom_id"])
                                        : 0,

                                quantity =
                                    reader.GetInt32(
                                        reader.GetOrdinal("quantity")
                                    ),

                                price =
                                    reader["price"] != DBNull.Value
                                        ? Convert.ToDecimal(reader["price"])
                                        : 0,

                                subtotal =
                                    reader["subtotal"] != DBNull.Value
                                        ? Convert.ToDecimal(reader["subtotal"])
                                        : 0
                            };

                            response.Add(detail);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                throw new Exception(msg);
            }

            return response;
        }

        // UPDATE PURCHASE ORDER TOTAL
        public async Task UpdatePurchaseOrderTotal(int purchaseOrderId)
        {
            const string query = @"
                UPDATE purchase_order
                SET total_amount =
                (
                    SELECT ISNULL(SUM(subtotal), 0)
                    FROM purchase_order_detail
                    WHERE purchase_order_id = @purchase_order_id
                )
                WHERE purchase_order_id = @purchase_order_id";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                await connection.OpenAsync();

                command.Parameters.AddWithValue(
                    "@purchase_order_id",
                    purchaseOrderId
                );

                await command.ExecuteNonQueryAsync();
            }
        }
    }
}