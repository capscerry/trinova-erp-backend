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
            return $"TRF-{DateTime.UtcNow.AddHours(7):yyyyMMddHHmmss}";
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
                destination_warehouse_id,
                status,
                processed_at,
                completed_at,
                canceled_at
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
                @destination_warehouse_id,
                @status,
                @processed_at,
                @completed_at,
                @canceled_at
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
            command.Parameters.AddWithValue("@movement_date", movement.movement_date ?? DateTime.UtcNow.AddHours(7));
            command.Parameters.AddWithValue("@created_by", movement.created_by ?? "");
            command.Parameters.AddWithValue("@created_at", movement.created_at ?? DateTime.UtcNow.AddHours(7));
            command.Parameters.AddWithValue("@source_warehouse_id", movement.source_warehouse_id);
            command.Parameters.AddWithValue("@destination_warehouse_id", movement.destination_warehouse_id ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@status", movement.status ?? "CREATED");
            command.Parameters.AddWithValue("@processed_at", movement.processed_at ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@completed_at", movement.completed_at ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@canceled_at", movement.canceled_at ?? (object)DBNull.Value);

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
                destination_warehouse_id,
                status
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
                @destination_warehouse_id,
                @status
            )";

            using var command = new SqlCommand(query, connection, transaction);

            command.Parameters.AddWithValue("@product_id", movement.product_id);
            command.Parameters.AddWithValue("@movement_type", movement.movement_type ?? "");
            command.Parameters.AddWithValue("@quantity", movement.quantity);
            command.Parameters.AddWithValue("@reference_number", movement.reference_number ?? "");
            command.Parameters.AddWithValue("@notes", movement.notes ?? "");
            command.Parameters.AddWithValue("@movement_date", movement.movement_date ?? DateTime.UtcNow.AddHours(7));
            command.Parameters.AddWithValue("@created_by", movement.created_by ?? "");
            command.Parameters.AddWithValue("@created_at", movement.created_at ?? DateTime.UtcNow.AddHours(7));
            command.Parameters.AddWithValue("@source_warehouse_id", (object?)movement.source_warehouse_id ?? DBNull.Value);
            command.Parameters.AddWithValue("@destination_warehouse_id", (object?)movement.destination_warehouse_id ?? DBNull.Value);
            command.Parameters.AddWithValue("@status", movement.status ?? "PROCESSED");

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<StockMovement>>
            GetByMovementTypeAsync(
                string movementType)
        {
            const string query = @"
            SELECT
                sm.*,
                mp.product_name,
                sw.warehouse_name AS source_warehouse_name,
                dw.warehouse_name AS destination_warehouse_name
            FROM stock_movement sm

            LEFT JOIN master_product mp
                ON sm.product_id = mp.product_id

            LEFT JOIN master_warehouse sw
                ON sm.source_warehouse_id = sw.warehouse_id

            LEFT JOIN master_warehouse dw
                ON sm.destination_warehouse_id = dw.warehouse_id

            WHERE sm.movement_type = @movement_type

            ORDER BY sm.movement_id DESC";

            var response = new List<StockMovement>();

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
                            Convert.ToInt32(reader["movement_id"]),

                        product_id =
                            Convert.ToInt32(reader["product_id"]),

                        product_name =
                            reader["product_name"]?.ToString(),

                        movement_type =
                            reader["movement_type"]?.ToString(),

                        quantity =
                            Convert.ToDecimal(reader["quantity"]),

                        reference_number =
                            reader["reference_number"]?.ToString(),

                        notes =
                            reader["notes"]?.ToString(),

                        source_warehouse_name =
                            reader["source_warehouse_name"]?.ToString(),

                        source_warehouse_id =
                            reader["source_warehouse_id"] == DBNull.Value
                                ? null
                                : Convert.ToInt32(reader["source_warehouse_id"]),

                        destination_warehouse_name =
                            reader["destination_warehouse_name"]?.ToString(),

                        destination_warehouse_id =
                            reader["destination_warehouse_id"] == DBNull.Value
                                ? null
                                : Convert.ToInt32(reader["destination_warehouse_id"]),

                        movement_date =
                            reader["movement_date"] == DBNull.Value
                                ? null
                                : Convert.ToDateTime(reader["movement_date"]),

                        processed_at =
                            reader["processed_at"] == DBNull.Value
                                ? null
                                : Convert.ToDateTime(reader["processed_at"]),

                        completed_at =
                            reader["completed_at"] == DBNull.Value
                                ? null
                                : Convert.ToDateTime(reader["completed_at"]),

                        canceled_at =
                            reader["canceled_at"] == DBNull.Value
                                ? null
                                : Convert.ToDateTime(reader["canceled_at"]),
                        
                        status =
                            reader["status"]?.ToString(),
                    });
            }

            return response;
        }

        public async Task<List<StockMovement>> GetAllAsync()
        {
            const string query = @"
            SELECT
                sm.*,
                mp.product_name,
                sw.warehouse_name AS source_warehouse_name,
                dw.warehouse_name AS destination_warehouse_name
            FROM stock_movement sm

            LEFT JOIN master_product mp
                ON sm.product_id = mp.product_id

            LEFT JOIN master_warehouse sw
                ON sm.source_warehouse_id = sw.warehouse_id

            LEFT JOIN master_warehouse dw
                ON sm.destination_warehouse_id = dw.warehouse_id

            WHERE sm.movement_type = 'TRANSFER'

            ORDER BY sm.movement_id DESC";

            var response = new List<StockMovement>();

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            await connection.OpenAsync();

            using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                response.Add(
                    new StockMovement
                    {
                        movement_id =
                            Convert.ToInt32(reader["movement_id"]),

                        product_id =
                            Convert.ToInt32(reader["product_id"]),

                        product_name =
                            reader["product_name"]?.ToString(),

                        movement_type =
                            reader["movement_type"]?.ToString(),

                        quantity =
                            Convert.ToDecimal(reader["quantity"]),

                        reference_number =
                            reader["reference_number"]?.ToString(),

                        notes =
                            reader["notes"]?.ToString(),

                        source_warehouse_name =
                            reader["source_warehouse_name"]?.ToString(),

                        source_warehouse_id =
                            reader["source_warehouse_id"] == DBNull.Value
                                ? null
                                : Convert.ToInt32(reader["source_warehouse_id"]),

                        destination_warehouse_name =
                            reader["destination_warehouse_name"]?.ToString(),

                        destination_warehouse_id =
                            reader["destination_warehouse_id"] == DBNull.Value
                                ? null
                                : Convert.ToInt32(reader["destination_warehouse_id"]),

                        movement_date =
                            reader["movement_date"] == DBNull.Value
                                ? null
                                : Convert.ToDateTime(reader["movement_date"]),
                        
                        status =
                            reader["status"]?.ToString(),

                        processed_at =
                            reader["processed_at"] == DBNull.Value
                                ? null
                                : Convert.ToDateTime(reader["processed_at"]),

                        completed_at =
                            reader["completed_at"] == DBNull.Value
                                ? null
                                : Convert.ToDateTime(reader["completed_at"]),

                        canceled_at =
                            reader["canceled_at"] == DBNull.Value
                                ? null
                                : Convert.ToDateTime(reader["canceled_at"]),
                    });
            }

            return response;
        }

        public async Task<StockMovement?> GetByIdAsync(int movementId)
        {
            const string query = @"
            SELECT
                sm.*,
                mp.product_name,
                sw.warehouse_name AS source_warehouse_name,
                dw.warehouse_name AS destination_warehouse_name
            FROM stock_movement sm

            LEFT JOIN master_product mp
                ON sm.product_id = mp.product_id

            LEFT JOIN master_warehouse sw
                ON sm.source_warehouse_id = sw.warehouse_id

            LEFT JOIN master_warehouse dw
                ON sm.destination_warehouse_id = dw.warehouse_id

            WHERE sm.movement_id = @movement_id";

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            command.Parameters.AddWithValue(
                "@movement_id",
                movementId);

            await connection.OpenAsync();

            using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new StockMovement
            {
                movement_id =
                    Convert.ToInt32(reader["movement_id"]),

                product_id =
                    Convert.ToInt32(reader["product_id"]),

                product_name =
                    reader["product_name"]?.ToString(),

                movement_type =
                    reader["movement_type"]?.ToString(),

                quantity =
                    Convert.ToDecimal(reader["quantity"]),

                reference_number =
                    reader["reference_number"]?.ToString(),

                notes =
                    reader["notes"]?.ToString(),

                source_warehouse_id =
                    reader["source_warehouse_id"] == DBNull.Value
                        ? null
                        : Convert.ToInt32(reader["source_warehouse_id"]),

                destination_warehouse_id =
                    reader["destination_warehouse_id"] == DBNull.Value
                        ? null
                        : Convert.ToInt32(reader["destination_warehouse_id"]),

                source_warehouse_name =
                    reader["source_warehouse_name"]?.ToString(),

                destination_warehouse_name =
                    reader["destination_warehouse_name"]?.ToString(),

                movement_date =
                    reader["movement_date"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(reader["movement_date"]),

                created_at =
                    reader["created_at"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(reader["created_at"]),

                status =
                    reader["status"]?.ToString(),

                processed_at =
                    reader["processed_at"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(reader["processed_at"]),

                completed_at =
                    reader["completed_at"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(reader["completed_at"]),

                canceled_at =
                    reader["canceled_at"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(reader["canceled_at"]),
            };
        }

        public async Task UpdateStatusAsync(int movementId, string status)
        {
            status = status.ToUpper();

            string query = "";

            if (status == "PROCESSED")
            {
                query = @"
                UPDATE stock_movement
                SET
                    status = @status,
                    processed_at = @current_time
                WHERE movement_id = @movement_id";
            }
            else if (status == "COMPLETED")
            {
                query = @"
                UPDATE stock_movement
                SET
                    status = @status,
                    completed_at = @current_time
                WHERE movement_id = @movement_id";
            }
            else if (status == "CANCELED")
            {
                query = @"
                UPDATE stock_movement
                SET
                    status = @status,
                    canceled_at = @current_time
                WHERE movement_id = @movement_id";
            }

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@movement_id", movementId);
            command.Parameters.AddWithValue("@status", status);
            command.Parameters.AddWithValue("@current_time", DateTime.UtcNow.AddHours(7));

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }
    }
}