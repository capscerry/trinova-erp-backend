using Microsoft.EntityFrameworkCore;
using trinova_erp_backend.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    "Server=trinova-server-dev.database.windows.net,1433;" +
    "Initial Catalog=trinova-dev;" +
    "User ID=capscerry;" +
    "Password=capstoneckrtgr@11;" +
    "Encrypt=True;" +
    "TrustServerCertificate=True;";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();

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