using ProductionCalculator.Data.Extensions;
using Resend;

namespace ProductionCalculator.API.Helpers
{
    public static class ConfigurationHelper
    {
        public static void SetupConnectionString(WebApplicationBuilder builder)
        {
            // Assemble connection string for local development
            // Otherwise, the full connection string should be provided in the environment through Docker Compose
            var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
            if (!isDocker)
            {
                var config = builder.Configuration;
                var password = config["DevDatabase:ServerPassword"] ?? "x";
                var baseConnStr = config.GetConnectionString("DefaultConnection") ?? "";
                // Replace password placeholder (e.g., Password=x) with actual secret
                var connectionString = baseConnStr.Replace("Password=x", $"Password={password}");
                config["ConnectionStrings:DefaultConnection"] = connectionString;
                builder.Services.AddProductionCalculatorData(config);
                Console.WriteLine("Connection String: " + connectionString);
            }
            else
            {
                builder.Services.AddProductionCalculatorData(builder.Configuration);
            }
        }

        public static void SetupResend(WebApplicationBuilder builder)
        {
            var config = builder.Configuration;
            var apiToken = config["RESEND_APITOKEN"] // For locally set environment variable
                ?? (File.Exists("/run/secrets/resend_apitoken") ? File.ReadAllText("/run/secrets/resend_apitoken") : null);  // For Docker secret
            if (string.IsNullOrEmpty(apiToken))
            {
                throw new InvalidOperationException("Resend API token is not set in environment variables.");
            }
            apiToken = apiToken.Trim();


            builder.Services.AddOptions();
            builder.Services.AddHttpClient<ResendClient>();
            builder.Services.Configure<ResendClientOptions>( o =>
            {
                o.ApiToken = apiToken;
            } );
            builder.Services.AddTransient<IResend, ResendClient>();
        }
    }
}
