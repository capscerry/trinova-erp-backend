using DotNetEnv;
using Microsoft.EntityFrameworkCore;
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