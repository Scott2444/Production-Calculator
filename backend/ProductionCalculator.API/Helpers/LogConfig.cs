using Serilog;
using Serilog.Formatting.Json;
using Serilog.Events;

namespace ProductionCalculator.API.Helpers
{
    public static class LogConfig
    {
        public static void SetupLogConfig(WebApplicationBuilder builder)
        {
            var env = builder.Environment.EnvironmentName;

            // Configure Serilog as the logging provider
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Is(builder.Environment.IsDevelopment() ? LogEventLevel.Information : LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Cors.Infrastructure.CorsService", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Mvc.Infrastructure.ObjectResultExecutor", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Environment", env);

            var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
            if (isDocker)
            {
                loggerConfig.WriteTo.Console(new JsonFormatter());
            }
            else
            {
                loggerConfig.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
            }

            Log.Logger = loggerConfig.CreateLogger();
            builder.Host.UseSerilog();
        }
        public static void SetupApiLogging(WebApplication app)
        {
            app.UseSerilogRequestLogging(options =>
            {
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("Method", httpContext.Request.Method);
                    
                    var endpoint = httpContext.GetEndpoint();
                    if (endpoint is RouteEndpoint routeEndpoint)
                    {
                        diagnosticContext.Set("RouteTemplate", routeEndpoint.RoutePattern.RawText);
                    }
                };
            });
        }
    }
}
