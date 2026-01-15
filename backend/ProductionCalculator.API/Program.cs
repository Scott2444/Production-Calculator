using System.Text;
using Microsoft.OpenApi.Models;
using ProductionCalculator.API.Helpers;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Services;
using ProductionCalculator.Business.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using ProductionCalculator.API.Authorization;

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
CorsPolicyHelper.SetupCorsPolicy(builder);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IMachineService, MachineService>();
builder.Services.AddSingleton<JwtHelper>();
builder.Services.AddSingleton<CookieOptionsHelper>();
builder.Services.AddSingleton<RefreshTokenHelper>();
builder.Services.AddSingleton<IAuthorizationHandler, UserHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, OwnerHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, OwnerOrAdminHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, PublicHandler>();
builder.Services.AddJwtAuthAndPolicies(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "api/swagger/{documentName}/swagger.json";
    });
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("v1/swagger.json", "ProductionCalculator API v1");
        c.RoutePrefix = "api/swagger";
    });
}

app.UseCors("_myAllowSpecificOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
