using ProductionCalculator.Data.Extensions;

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
            }
            else
            {
                builder.Services.AddProductionCalculatorData(builder.Configuration);
            }
        }
    }
}
