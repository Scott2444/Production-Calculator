using Microsoft.OpenApi.Models;
using ProductionCalculator.Data.Extensions;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ProductionCalculator API", Version = "v1" });
});

// Add application services and data
// Assemble connection string for local development
// Otherwise, the full connection string should be provided in the environment
var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
Console.WriteLine($"Is Docker: {isDocker}");
if (!isDocker)
{
    var config = builder.Configuration;
    var password = config["DevDatabase:ServerPassword"] ?? "x";
    var baseConnStr = config.GetConnectionString("DefaultConnection") ?? "";
    // Replace password placeholder (e.g., Password=x) with actual secret
    var connectionString = baseConnStr.Replace("Password=x", $"Password={password}");
    config["ConnectionStrings:DefaultConnection"] = connectionString;
    builder.Services.AddProductionCalculatorData(config);
}
else
{
    // Use the full connection string from configuration/environment
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
    builder.Services.AddProductionCalculatorData(builder.Configuration);
}
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProductionCalculator API v1"));
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
