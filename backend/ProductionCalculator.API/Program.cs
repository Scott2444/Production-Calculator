using Microsoft.OpenApi.Models;
using ProductionCalculator.Data.Extensions;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Services;

var builder = WebApplication.CreateBuilder(args);

// Load local secrets file if present (not checked into source control)
var secretsPath = Path.Combine(builder.Environment.ContentRootPath, "..", "secrets", "appsettings.Secrets.json");
if (File.Exists(secretsPath))
{
    builder.Configuration.AddJsonFile(secretsPath, optional: true, reloadOnChange: true);
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ProductionCalculator API", Version = "v1" });
});

// Add application services and data
builder.Services.AddProductionCalculatorData(builder.Configuration);
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ProductionCalculator.Business.Interfaces.IUserService, ProductionCalculator.Business.Services.UserService>();

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
