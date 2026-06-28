using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using trinova_erp_backend.Config;
using trinova_erp_backend.Data;
using trinova_erp_backend.Repositories.Persediaan;
using trinova_erp_backend.Usecase.Persediaan;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DatabaseConnection>(options =>
{
    options.SQLServer = Env.GetString("SQL_CONNECTION_STRING_DEV");
});


builder.Services.Configure<JwtSettings>(options =>
{
    options.Secret = Env.GetString("JWT_SECRET_KEY");
    options.Issuer = Env.GetString("JWT_ISSUER");
    options.Audience = Env.GetString("JWT_AUDIENCE");
    options.ExpirationMinutes = int.TryParse(Env.GetString("JWT_EXPIRE_MINUTES", "60"), out var exp) ? exp : 60;
});



builder.Services.AddApplicationServices();
builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<MasterProductSubcategoryRepo>();
builder.Services.AddScoped<MasterProductSubcategoryUsecase>();
builder.Services.AddScoped<StockMovementRepo>();
builder.Services.AddScoped<StockTransferUsecase>();
builder.Services.AddScoped<OrderFulfillmentUsecase>();
builder.Services.AddScoped<ForecastRepo>();
builder.Services.AddScoped<DemandForecastUsecase>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowCors", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

await EnsureSalesStatusColumnsAsync(Env.GetString("SQL_CONNECTION_STRING_DEV"));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowCors");

app.UseAuthorization();

app.MapControllers();

app.Run();

static async Task EnsureSalesStatusColumnsAsync(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
        return;

    var targets = new[]
    {
        ("sales_quotation", "status", "update_date"),
        ("sales_order", "status", "updated_at"),
        ("uang_muka", "status", "updated_at"),
        ("delivery_order_header", "status", "updated_at"),
        ("sales_invoice", "status", "updated_at"),
        ("sales_receipt", "status", "updated_at")
    };

    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();

    foreach (var (table, statusColumn, updatedAtColumn) in targets)
    {
        await using (var statusCommand = connection.CreateCommand())
        {
            statusCommand.CommandText = $@"
                IF COL_LENGTH('{table}', '{statusColumn}') IS NULL
                BEGIN
                    ALTER TABLE {table}
                    ADD {statusColumn} VARCHAR(30) NOT NULL
                    CONSTRAINT DF_{table}_{statusColumn} DEFAULT 'Draft'
                END";
            await statusCommand.ExecuteNonQueryAsync();
        }

        await using var updatedAtCommand = connection.CreateCommand();
        updatedAtCommand.CommandText = $@"
            IF COL_LENGTH('{table}', '{updatedAtColumn}') IS NULL
            BEGIN
                ALTER TABLE {table}
                ADD {updatedAtColumn} DATETIME NULL
            END";
        await updatedAtCommand.ExecuteNonQueryAsync();
    }
}
