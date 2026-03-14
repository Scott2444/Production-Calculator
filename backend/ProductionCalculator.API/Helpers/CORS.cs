namespace ProductionCalculator.API.Helpers
{
    public static class CorsPolicyHelper
    {
        public static void SetupCorsPolicy(WebApplicationBuilder builder)
        {
            var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: MyAllowSpecificOrigins,
                                policy =>
                                {
                                    policy.WithOrigins(
                                                       "http://localhost:3000",
                                                       "https://production-calculator.com",
                                                       "https://www.production-calculator.com",
                                                       "https://production-calculator-dev.pages.dev",
                                                       "https://production-calculator-staging.pages.dev",
                                                       "https://production-calculator-prod.pages.dev",
                                                       "https://*.production-calculator.com")
                                            .SetIsOriginAllowedToAllowWildcardSubdomains()
                                            .AllowAnyHeader()
                                            .AllowAnyMethod()
                                            .AllowCredentials();
                                });
            });

        }
    }
}
