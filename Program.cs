using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using trinova_erp_backend.Config;
using trinova_erp_backend.Data;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DatabaseConnection>(options =>
{
    options.SQLServer = Env.GetString("SQL_CONNECTION_STRING_DEV");
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        Env.GetString("SQL_CONNECTION_STRING_DEV")));

builder.Services.AddApplicationServices();
builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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