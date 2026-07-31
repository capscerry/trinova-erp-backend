using DotNetEnv;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using trinova_erp_backend.Config;
using trinova_erp_backend.Data;
using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Persediaan;
using trinova_erp_backend.Services;
using trinova_erp_backend.Services.InventoryAI;
using trinova_erp_backend.Services.PurchasingAI;
using trinova_erp_backend.Usecase.Persediaan;

// ── 1. Load .env only when it exists (local dev only) ────────────────────────
//      Railway injects variables directly into the process environment;
//      a missing .env file must never crash the container.
if (File.Exists(".env"))
{
    Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

// ── 2. Centralise configuration: IConfiguration first, Env as fallback ───────
//      Railway sets env-vars that ASP.NET Core's default IConfiguration provider
//      already exposes, so builder.Configuration["KEY"] is the authoritative
//      source.  Env.GetString is kept only as a fallback for local .env usage.
var configuration = builder.Configuration;

// Performance: Raw environment dump is restricted to Development only.
// In Production (Railway) this would print on every cold start and expose
// variable names to container logs unnecessarily. Validation still runs below.
if (builder.Environment.IsDevelopment())
{
    Console.WriteLine("===== RAW ENVIRONMENT =====");
    Console.WriteLine($"SQL_CONNECTION_STRING_DEV = {Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING_DEV")}");
    Console.WriteLine($"JWT_SECRET_KEY            = {Environment.GetEnvironmentVariable("JWT_SECRET_KEY")}");
    Console.WriteLine($"JWT_ISSUER                = {Environment.GetEnvironmentVariable("JWT_ISSUER")}");
    Console.WriteLine($"JWT_AUDIENCE              = {Environment.GetEnvironmentVariable("JWT_AUDIENCE")}");
    Console.WriteLine($"JWT_EXPIRE_MINUTES        = {Environment.GetEnvironmentVariable("JWT_EXPIRE_MINUTES")}");
    Console.WriteLine("===========================");
}

var connectionString =
    Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING_DEV")
    ?? configuration["SQL_CONNECTION_STRING_DEV"]
    ?? Env.GetString("SQL_CONNECTION_STRING_DEV");

var jwtSecret =
    Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? configuration["JWT_SECRET_KEY"]
    ?? Env.GetString("JWT_SECRET_KEY");

var jwtIssuer =
    Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? configuration["JWT_ISSUER"]
    ?? Env.GetString("JWT_ISSUER");

var jwtAudience =
    Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? configuration["JWT_AUDIENCE"]
    ?? Env.GetString("JWT_AUDIENCE");

var jwtExpireRaw =
    configuration["JWT_EXPIRE_MINUTES"]
    ?? Env.GetString("JWT_EXPIRE_MINUTES");

var xgboostUrl =
    configuration["ExternalServices:XGBoostApiUrl"]
    ?? Env.GetString("XGBOOST_API_URL")
    ?? "http://127.0.0.1:8000";

var smtpHost        = configuration["SMTP_HOST"]         ?? Env.GetString("SMTP_HOST")         ?? "smtp.gmail.com";
var smtpPortRaw     = configuration["SMTP_PORT"]         ?? Env.GetString("SMTP_PORT");
var smtpUser        = configuration["SMTP_USER"]         ?? Env.GetString("SMTP_USER");
var smtpAppPassword = configuration["SMTP_APP_PASSWORD"] ?? Env.GetString("SMTP_APP_PASSWORD");
var smtpFromName    = configuration["SMTP_FROM_NAME"]    ?? Env.GetString("SMTP_FROM_NAME")    ?? "Trinova ERP";

// Purchasing AI base URL — configurable via appsettings or environment variable.
// Environment variable name: ExternalServices__PurchasingAIBaseUrl
// Falls back to the XGBoost URL so existing behaviour is preserved when the
// new key is absent (e.g. older deployments that haven't set the env var yet).
var purchasingAIBaseUrl =
    configuration["ExternalServices:PurchasingAIBaseUrl"]
    ?? Environment.GetEnvironmentVariable("ExternalServices__PurchasingAIBaseUrl")
    ?? xgboostUrl;

// Inventory AI base URL — points to the demand-forecast Railway service.
// Environment variable name: ExternalServices__InventoryAIBaseUrl
// Falls back to the shared xgboostUrl only as a last resort so older
// deployments without this env var don't crash at startup.
var inventoryAIBaseUrl =
    configuration["ExternalServices:InventoryAIBaseUrl"]
    ?? Environment.GetEnvironmentVariable("ExternalServices__InventoryAIBaseUrl")
    ?? "https://trinova-ai-production.up.railway.app";

// ── 3. Validate required configuration at startup ─────────────────────────────
//      Fail fast with a clear message instead of a cryptic NullReferenceException
//      or Encoding.GetBytes crash later.
var missingKeys = new List<string>();
if (string.IsNullOrWhiteSpace(connectionString)) missingKeys.Add("SQL_CONNECTION_STRING_DEV");
if (string.IsNullOrWhiteSpace(jwtSecret))        missingKeys.Add("JWT_SECRET_KEY");
if (string.IsNullOrWhiteSpace(jwtIssuer))        missingKeys.Add("JWT_ISSUER");
if (string.IsNullOrWhiteSpace(jwtAudience))      missingKeys.Add("JWT_AUDIENCE");

if (missingKeys.Count > 0)
{
    throw new InvalidOperationException(
        $"Missing configuration: {string.Join(", ", missingKeys)}. " +
        "Set these as Railway environment variables (or in .env for local dev).");
}

// ── 9. Startup diagnostics – presence only, never print secret values ─────────
var port = Environment.GetEnvironmentVariable("PORT");

// Performance: Restrict startup diagnostics to Development only.
// Railway cold-starts do not need this console output. Validation above still
// runs unconditionally — only the informational output is gated. Business logic
// and configuration loading are completely unchanged.
if (builder.Environment.IsDevelopment())
{
    Console.WriteLine($"Environment  : {builder.Environment.EnvironmentName}");
    Console.WriteLine($"PORT         : {(string.IsNullOrEmpty(port) ? "(not set – Kestrel default)" : port)}");
    Console.WriteLine($"SQL Loaded   : {!string.IsNullOrWhiteSpace(connectionString)}");
    Console.WriteLine($"JWT Loaded   : {!string.IsNullOrWhiteSpace(jwtSecret)}");
    Console.WriteLine($"XGBoost URL  : {xgboostUrl}");
    Console.WriteLine($"Purchasing AI: {purchasingAIBaseUrl}");
    Console.WriteLine($"Inventory AI : {inventoryAIBaseUrl}");
}

// ── PORT binding (Railway sets PORT) ─────────────────────────────────────────
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// ── Service registrations ─────────────────────────────────────────────────────

builder.Services.Configure<DatabaseConnection>(options =>
{
    options.SQLServer = connectionString;
});

builder.Services.Configure<JwtSettings>(options =>
{
    options.Secret             = jwtSecret!;
    options.Issuer             = jwtIssuer!;
    options.Audience           = jwtAudience!;
    options.ExpirationMinutes  = int.TryParse(jwtExpireRaw, out var exp) ? exp : 60;
});

builder.Services.Configure<EmailSettings>(options =>
{
    options.Host        = smtpHost!;
    options.Port        = int.TryParse(smtpPortRaw, out var p) ? p : 587;
    options.User        = smtpUser ?? string.Empty;
    options.AppPassword = smtpAppPassword ?? string.Empty;
    options.FromName    = smtpFromName!;
});

builder.Services.AddApplicationServices();
builder.Services.AddMemoryCache();

// ── Performance: Response Compression ────────────────────────────────────────
// Reduces response payload sizes for JSON API responses, lowering bandwidth
// and improving client-perceived latency, especially on Railway's public edge.
// EnableForHttps is required because Railway terminates TLS upstream and the
// app itself runs on plain HTTP inside the container. Business logic unchanged.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json" });
});

// ── Performance: Response Caching ────────────────────────────────────────────
// Registers the caching infrastructure so [ResponseCache] attributes on
// individual endpoints can be applied safely. No endpoint is cached by default;
// this only enables opt-in caching per-action. Business logic unchanged.
builder.Services.AddResponseCaching();

// File-upload hardening: cap multipart body size before IFormFile is populated.
// FileUploadSecurity.ValidateExcel/ValidateCsv enforce the same ceiling per-endpoint.
var maxUploadBytes = builder.Configuration.GetValue<long>(
    "FileUploadSecurity:MaxRequestBodyBytes", 15 * 1024 * 1024);

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxUploadBytes;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxUploadBytes;
});

// Named HttpClient for the XGBoost FastAPI service (config-driven, not hardcoded).
// Performance: Uses IHttpClientFactory to reuse socket connections, preventing
// socket exhaustion under load. Timeout is kept at 60 s for ML inference calls.
builder.Services.AddHttpClient("XGBoost", (serviceProvider, client) =>
{
    client.BaseAddress = new Uri(xgboostUrl);
    client.Timeout     = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ── ForecastClient — Inventory AI service (trinova-ai-production.up.railway.app) ──
// Typed HttpClient via IHttpClientFactory for proper connection pooling.
// BaseAddress now correctly points to InventoryAIBaseUrl, not the Purchasing AI.
// Timeout is 30 s — InventoryAIService applies its own per-request CTS as well.
builder.Services.AddHttpClient<ForecastClient>(client =>
{
    client.BaseAddress = new Uri(inventoryAIBaseUrl);
    client.Timeout     = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ── IInventoryAIService — typed HttpClient for /api/inventory-ai/* endpoints ──
// Separate registration from ForecastClient so the two consumers get independent
// HttpClient instances with their own connection pools and lifecycle management.
builder.Services.AddHttpClient<IInventoryAIService, InventoryAIService>(client =>
{
    client.BaseAddress = new Uri(inventoryAIBaseUrl);
    client.Timeout     = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ── IPurchasingAIService — typed HttpClient for /api/purchasing-ai/* endpoints ─
builder.Services.AddHttpClient<IPurchasingAIService, PurchasingAIService>(client =>
{
    client.BaseAddress = new Uri(purchasingAIBaseUrl);
    client.Timeout     = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddScoped<MasterProductSubcategoryRepo>();
builder.Services.AddScoped<MasterProductSubcategoryUsecase>();
builder.Services.AddScoped<StockMovementRepo>();
builder.Services.AddScoped<StockTransferUsecase>();
builder.Services.AddScoped<OrderFulfillmentUsecase>();
builder.Services.AddScoped<DemandForecastUsecase>();
builder.Services.AddScoped<ForecastExcelExporter>();
builder.Services.AddScoped<PurchaseRequisitionDetailRepo>();
builder.Services.AddScoped<InventoryDashboardUsecase>();
builder.Services.AddScoped<ForecastDatasetRepo>();

// ── Single call to AddControllers ────────────────────────────────────────────
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowCors", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "https://trinova-erp-frontend-mu.vercel.app")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ── JWT authentication ────────────────────────────────────────────────────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken            = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret!)),
            ValidateIssuer           = true,
            ValidIssuer              = jwtIssuer,
            ValidateAudience         = true,
            ValidAudience            = jwtAudience,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdClaim = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdClaim, out var userId))
                {
                    context.Fail("Invalid user token.");
                    return;
                }

                // Use the centralised connectionString variable, not Env.GetString.
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT is_active FROM master_user WHERE id = @UserId";
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value || !Convert.ToBoolean(result))
                {
                    var logger = context.HttpContext.RequestServices.GetService<IActivityLogService>();
                    if (logger != null)
                    {
                        await logger.LogAsync(new ActivityLogCreate
                        {
                            Module       = "security",
                            ActivityType = "inactive_user_token_rejected",
                            Title        = "Inactive user token rejected",
                            Description  = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}",
                            RefTable     = "master_user",
                            RefId        = userId,
                            RefNumber    = context.HttpContext.Request.Path
                        });
                    }

                    context.Fail("User account is inactive.");
                }
            },
            OnChallenge = async context =>
            {
                try
                {
                    var logger = context.HttpContext.RequestServices.GetService<IActivityLogService>();
                    if (logger != null)
                    {
                        await logger.LogAsync(new ActivityLogCreate
                        {
                            Module       = "security",
                            ActivityType = "authentication_required",
                            Title        = "Authentication required",
                            Description  = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}",
                            RefTable     = "api_endpoint",
                            RefNumber    = context.HttpContext.Request.Path
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
                            Module       = "security",
                            ActivityType = "unauthorized_access",
                            Title        = "Unauthorized access attempt",
                            Description  = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}",
                            RefTable     = "api_endpoint",
                            RefNumber    = context.HttpContext.Request.Path
                        });
                    }
                }
                catch { /* Never crash the request pipeline over a logging failure */ }
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ── Startup migration: ensure supplier_code_counter exists ───────────────────
// This is the idempotent equivalent of running add_supplier_code_sequence.sql.
// It runs once at cold-start before the app serves any traffic, so the table
// will always exist on Azure SQL without requiring a manual database edit.
// All statements are guarded with IF NOT EXISTS / IF EXISTS so re-running on
// an already-migrated database is completely safe.
try
{
    await using var startupConn = new SqlConnection(connectionString);
    await startupConn.OpenAsync();

    // Step 1 — create the table if it does not already exist
    const string createTable = @"
        IF OBJECT_ID(N'dbo.supplier_code_counter', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.supplier_code_counter
            (
                id         TINYINT NOT NULL CONSTRAINT PK_supplier_code_counter PRIMARY KEY,
                last_value BIGINT  NOT NULL
            );
        END;";

    await using (var cmd = new SqlCommand(createTable, startupConn))
        await cmd.ExecuteNonQueryAsync();

    // Step 2 — seed from the highest existing well-formed supplier code,
    //          but only if no seed row exists yet (idempotent)
    const string seedRow = @"
        IF NOT EXISTS (SELECT 1 FROM dbo.supplier_code_counter WHERE id = 1)
        BEGIN
            DECLARE @seed BIGINT;
            SELECT @seed = ISNULL(
                (
                    SELECT MAX(CAST(SUBSTRING(supplier_code, 5, 10) AS BIGINT))
                    FROM   dbo.master_supplier
                    WHERE  supplier_code LIKE
                        'SUP-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
                ),
                0
            );
            INSERT INTO dbo.supplier_code_counter (id, last_value)
            VALUES (1, @seed);
        END;";

    await using (var cmd = new SqlCommand(seedRow, startupConn))
        await cmd.ExecuteNonQueryAsync();

    app.Logger.LogInformation("supplier_code_counter migration: OK");
}
catch (Exception ex)
{
    // Log and continue — a startup migration failure should not silently eat
    // the real error, but we also must not crash the entire app over this.
    app.Logger.LogError(ex, "supplier_code_counter startup migration FAILED. " +
        "Supplier code generation will be unavailable until this is resolved. " +
        "Run Migrations/add_supplier_code_sequence.sql manually as a fallback.");
}

// ── 5. Swagger always enabled (accessible on Railway) ────────────────────────
app.UseSwagger();
app.UseSwaggerUI();

// ── Performance: Response Compression middleware ──────────────────────────────
// Must be registered before any middleware that writes response bodies so that
// the compressor can wrap the response stream. Does not affect response content.
app.UseResponseCompression();

// ── 6. HTTPS redirection disabled in Production (Railway terminates TLS) ─────
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

// ── Global exception handler ──────────────────────────────────────────────────
// MUST be registered AFTER UseCors so that CORS headers written by the CORS
// middleware survive when an unhandled exception occurs.  Without this, Kestrel
// returns a raw 500 with no CORS headers and the browser reports a CORS error
// even though CORS is correctly configured.
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async ctx =>
    {
        var feature = ctx.Features
            .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var ex = feature?.Error;

        var logger = ctx.RequestServices
            .GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Unhandled exception on {Method} {Path}",
            ctx.Request.Method, ctx.Request.Path);

        ctx.Response.StatusCode  = StatusCodes.Status500InternalServerError;
        ctx.Response.ContentType = "application/json";

        await ctx.Response.WriteAsJsonAsync(new
        {
            error   = "An unexpected server error occurred.",
            detail  = app.Environment.IsDevelopment() ? ex?.ToString() : null,
            path    = ctx.Request.Path.Value,
            traceId = System.Diagnostics.Activity.Current?.Id
                      ?? ctx.TraceIdentifier
        });
    });
});

app.UseCors("AllowCors");

// ── Performance: Response Caching middleware ──────────────────────────────────
// Must come after UseCors and before UseAuthentication so the cache key
// includes any Vary headers. No endpoint is cached unless explicitly decorated
// with [ResponseCache]. Business logic and auth flow unchanged.
app.UseResponseCaching();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
