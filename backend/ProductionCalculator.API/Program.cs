using Microsoft.OpenApi.Models;
using ProductionCalculator.API.Helpers;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Services;
using ProductionCalculator.Business.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ProductionCalculator API", Version = "v1" });
});

// Add application services and data
ConfigurationHelper.SetupConnectionString(builder);
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<JwtHelper>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProductionCalculator API v1"));
}

app.UseAuthorization();

app.MapControllers();

app.Run();
