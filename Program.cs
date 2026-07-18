using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using trinova_erp_backend.Config;
using trinova_erp_backend.Data;
using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Persediaan;
using trinova_erp_backend.Services;
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
builder.Services.AddMemoryCache();

// Named HttpClient for the XGBoost FastAPI service.
// Base URL is read from appsettings.json → ExternalServices:XGBoostApiUrl
builder.Services.AddHttpClient("XGBoost", (serviceProvider, client) =>
{
    var config  = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = config["ExternalServices:XGBoostApiUrl"] ?? "http://localhost:8000";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout     = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddControllers();

var jwtSecret = Env.GetString("JWT_SECRET_KEY");
var jwtIssuer = Env.GetString("JWT_ISSUER");
var jwtAudience = Env.GetString("JWT_AUDIENCE");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                try
                {
                    var logger = context.HttpContext.RequestServices.GetService<IActivityLogService>();
                    if (logger != null)
                    {
                        await logger.LogAsync(new ActivityLogCreate
                        {
                            Module = "security",
                            ActivityType = "authentication_required",
                            Title = "Authentication required",
                            Description = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}",
                            RefTable = "api_endpoint",
                            RefNumber = context.HttpContext.Request.Path
                        });
                    }
                }
                catch { /* Never crash the request pipeline over a logging failure */ }
            },
            OnForbidden = async context =>
            {
                try
                {
                    var logger = context.HttpContext.RequestServices.GetService<IActivityLogService>();
                    if (logger != null)
                    {
                        await logger.LogAsync(new ActivityLogCreate
                        {
                            Module = "security",
                            ActivityType = "unauthorized_access",
                            Title = "Unauthorized access attempt",
                            Description = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}",
                            RefTable = "api_endpoint",
                            RefNumber = context.HttpContext.Request.Path
                        });
                    }
                }
                catch { /* Never crash the request pipeline over a logging failure */ }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
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

//await EnsureSalesStatusColumnsAsync(Env.GetString("SQL_CONNECTION_STRING_DEV"));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

//static async Task EnsureSalesStatusColumnsAsync(string? connectionString)
//{
//    if (string.IsNullOrWhiteSpace(connectionString))
//        return;

//    var targets = new[]
//    {
//        ("sales_quotation", "status", "update_date"),
//        ("sales_order", "status", "updated_at"),
//        ("uang_muka", "status", "updated_at"),
//        ("delivery_order_header", "status", "updated_at"),
//        ("sales_invoice", "status", "updated_at"),
//        ("sales_receipt", "status", "updated_at")
//    };

//    await using var connection = new SqlConnection(connectionString);
//    await connection.OpenAsync();

//    foreach (var (table, statusColumn, updatedAtColumn) in targets)
//    {
//        await using (var statusCommand = connection.CreateCommand())
//        {
//            statusCommand.CommandText = $@"
//                IF COL_LENGTH('{table}', '{statusColumn}') IS NULL
//                BEGIN
//                    ALTER TABLE {table}
//                    ADD {statusColumn} VARCHAR(30) NOT NULL
//                    CONSTRAINT DF_{table}_{statusColumn} DEFAULT 'Draft'
//                END";
//            await statusCommand.ExecuteNonQueryAsync();
//        }

//        await using var updatedAtCommand = connection.CreateCommand();
//        updatedAtCommand.CommandText = $@"
//            IF COL_LENGTH('{table}', '{updatedAtColumn}') IS NULL
//            BEGIN
//                ALTER TABLE {table}
//                ADD {updatedAtColumn} DATETIME NULL
//            END";
//        await updatedAtCommand.ExecuteNonQueryAsync();
//    }
//}
