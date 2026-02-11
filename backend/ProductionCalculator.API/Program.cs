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
ConfigurationHelper.SetupResend(builder);
CorsPolicyHelper.SetupCorsPolicy(builder);
RateLimitConfig.AddRateLimiting(builder.Services, builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IMachineService, MachineService>();
builder.Services.AddScoped<IModifierService, ModifierService>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IWorkflowChartService, WorkflowChartService>();
builder.Services.AddScoped<IWorkflowChartDataService, WorkflowNodeDbService>();
builder.Services.AddScoped<IWorkflowSolver, WorkflowSolver>();
builder.Services.AddScoped<IProjectDataService, ProjectDataService>();
builder.Services.AddScoped<IMachineCalculator, MachineCalculator>();
builder.Services.AddScoped<IWorkflowChartAssembler, WorkflowChartAssembler>();
builder.Services.AddScoped<IWorkflowMapper, WorkflowMapper>();
builder.Services.AddScoped<IWorkflowChartValidator, WorkflowChartValidator>();
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
    app.Use(async (context, next) =>
    {
        var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("RequestTrace");

        logger.LogInformation(
            "HTTP {Method} {Path}{Query} UA:{UserAgent} Trace:{TraceId}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            context.Request.Headers.UserAgent.ToString(),
            context.TraceIdentifier);

        await next();

        logger.LogInformation(
            "=> {StatusCode} {Method} {Path}{Query} Trace:{TraceId}",
            context.Response.StatusCode,
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            context.TraceIdentifier);
    });

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

app.UseRateLimiter();

app.UseCors("_myAllowSpecificOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
