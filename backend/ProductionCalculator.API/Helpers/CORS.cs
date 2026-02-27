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
                                    policy.WithOrigins("http://localhost:3000",
                                                       "https://dev.production-calculator.com",
                                                       "https://staging.production-calculator.com",
                                                       "https://production-calculator.com",
                                                       "https://www.production-calculator.com")
                                            .AllowAnyHeader()
                                            .AllowAnyMethod()
                                            .AllowCredentials();
                                });
            });

        }
    }
}
