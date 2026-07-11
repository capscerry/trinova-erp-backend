using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    public class StockMovementRepo
    {
        private readonly string _connectionString;

        public StockMovementRepo(
            IOptionsSnapshot<DatabaseConnection> options)
        {
            _connectionString =
                options.Value.SQLServer!;
        }

        // GENERATE TRF NUMBER
        public async Task<string> GenerateTRFNumber()
        {
            const string query = @"
                SELECT TOP 1 reference_number
                FROM stock_movement
                WHERE movement_type = 'TRANSFER'
                ORDER BY movement_id DESC";

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            using SqlCommand command =
                new SqlCommand(query, connection);

            object? result =
                await command.ExecuteScalarAsync();

            int nextNumber = 1;

            if (result != null && result != DBNull.Value)
            {
                string lastTrf =
                    result.ToString() ?? "TRF000000";

                string numericPart =
                    lastTrf.Replace("TRF", "");

                if (int.TryParse(numericPart, out int parsed))
                    nextNumber = parsed + 1;
            }

            return $"TRF{nextNumber:D6}";
        }

        public async Task InsertAsync(
            StockMovement movement)
        {
            const string query = @"
            INSERT INTO stock_movement
            (
                product_id,
                movement_type,
                quantity,
                reference_number,
                notes,
                movement_date,
                created_by,
                created_at,
                source_warehouse_id,
                destination_warehouse_id
            )
            VALUES
            (
                @product_id,
                @movement_type,
                @quantity,
                @reference_number,
                @notes,
                @movement_date,
                @created_by,
                @created_at,
                @source_warehouse_id,
                @destination_warehouse_id
            )";

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@product_id", movement.product_id);
            command.Parameters.AddWithValue("@movement_type", movement.movement_type ?? "");
            command.Parameters.AddWithValue("@quantity", movement.quantity);
            command.Parameters.AddWithValue("@reference_number", movement.reference_number ?? "");
            command.Parameters.AddWithValue("@notes", movement.notes ?? "");
            command.Parameters.AddWithValue("@movement_date", movement.movement_date ?? DateTime.Now);
            command.Parameters.AddWithValue("@created_by", movement.created_by ?? "");
            command.Parameters.AddWithValue("@created_at", movement.created_at ?? DateTime.Now);
            command.Parameters.AddWithValue("@source_warehouse_id", (object?)movement.source_warehouse_id ?? DBNull.Value);
            command.Parameters.AddWithValue("@destination_warehouse_id", (object?)movement.destination_warehouse_id ?? DBNull.Value);

            await connection.OpenAsync();

            try
            {
                await command.ExecuteNonQueryAsync();
            }
            catch(Exception ex)
            {
                Console.WriteLine("INSERT STOCK MOVEMENT ERROR");
                Console.WriteLine(ex.Message);

                throw;
            }
        }

        // Same insert as InsertAsync, but runs on the caller's connection
        // and transaction so it's committed/rolled back atomically with
        // whatever stock change it's auditing (e.g. DO deduct).
        public async Task InsertAsync(
            StockMovement movement,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            const string query = @"
            INSERT INTO stock_movement
            (
                product_id,
                movement_type,
                quantity,
                reference_number,
                notes,
                movement_date,
                created_by,
                created_at,
                source_warehouse_id,
                destination_warehouse_id
            )
            VALUES
            (
                @product_id,
                @movement_type,
                @quantity,
                @reference_number,
                @notes,
                @movement_date,
                @created_by,
                @created_at,
                @source_warehouse_id,
                @destination_warehouse_id
            )";

            using var command = new SqlCommand(query, connection, transaction);

            command.Parameters.AddWithValue("@product_id", movement.product_id);
            command.Parameters.AddWithValue("@movement_type", movement.movement_type ?? "");
            command.Parameters.AddWithValue("@quantity", movement.quantity);
            command.Parameters.AddWithValue("@reference_number", movement.reference_number ?? "");
            command.Parameters.AddWithValue("@notes", movement.notes ?? "");
            command.Parameters.AddWithValue("@movement_date", movement.movement_date ?? DateTime.Now);
            command.Parameters.AddWithValue("@created_by", movement.created_by ?? "");
            command.Parameters.AddWithValue("@created_at", movement.created_at ?? DateTime.Now);
            command.Parameters.AddWithValue("@source_warehouse_id", (object?)movement.source_warehouse_id ?? DBNull.Value);
            command.Parameters.AddWithValue("@destination_warehouse_id", (object?)movement.destination_warehouse_id ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<StockMovement>>
            GetByMovementTypeAsync(
                string movementType)
        {
            const string query = @"
                SELECT *
                FROM stock_movement
                WHERE movement_type = @movement_type
                ORDER BY movement_id DESC";

            var response =
                new List<StockMovement>();

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            command.Parameters.AddWithValue(
                "@movement_type",
                movementType);

            await connection.OpenAsync();

            using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                response.Add(
                    new StockMovement
                    {
                        movement_id =
                            Convert.ToInt32(
                                reader["movement_id"]),

                        product_id =
                            Convert.ToInt32(
                                reader["product_id"]),

                        movement_type =
                            reader["movement_type"]
                                ?.ToString(),

                        quantity =
                            Convert.ToDecimal(
                                reader["quantity"]),

                        reference_number =
                            reader["reference_number"]
                                ?.ToString(),

                        notes =
                            reader["notes"]
                                ?.ToString(),

                        source_warehouse_id =
                            reader["source_warehouse_id"]
                            == DBNull.Value
                            ? null
                            : Convert.ToInt32(
                                reader["source_warehouse_id"]),

                        destination_warehouse_id =
                            reader["destination_warehouse_id"]
                            == DBNull.Value
                            ? null
                            : Convert.ToInt32(
                                reader["destination_warehouse_id"]),

                        movement_date =
                            reader["movement_date"]
                            == DBNull.Value
                            ? null
                            : Convert.ToDateTime(
                                reader["movement_date"])
                    });
            }

            return response;
        }

        public async Task<List<StockMovement>> GetAllAsync()
        {
            const string query = @"
                SELECT *
                FROM stock_movement
                WHERE movement_type = 'TRANSFER'
                ORDER BY movement_id DESC";

            var response = new List<StockMovement>();

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            await connection.OpenAsync();

            using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            while(await reader.ReadAsync())
            {
                response.Add(
                    new StockMovement
                    {
                        movement_id =
                            Convert.ToInt32(
                                reader["movement_id"]),

                        product_id =
                            Convert.ToInt32(
                                reader["product_id"]),

                        movement_type =
                            reader["movement_type"]
                                ?.ToString(),

                        quantity =
                            Convert.ToDecimal(
                                reader["quantity"]),

                        reference_number =
                            reader["reference_number"]
                                ?.ToString(),

                        notes =
                            reader["notes"]
                                ?.ToString(),

                        source_warehouse_id =
                            reader["source_warehouse_id"]
                            == DBNull.Value
                            ? null
                            : Convert.ToInt32(
                                reader["source_warehouse_id"]),

                        destination_warehouse_id =
                            reader["destination_warehouse_id"]
                            == DBNull.Value
                            ? null
                            : Convert.ToInt32(
                                reader["destination_warehouse_id"]),

                        movement_date =
                            reader["movement_date"]
                            == DBNull.Value
                            ? null
                            : Convert.ToDateTime(
                                reader["movement_date"])
                    });
            }
            return response;
        }
    }
}