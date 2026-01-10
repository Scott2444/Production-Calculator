using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ProductionCalculator.API.Authorization;

namespace ProductionCalculator.API.Helpers
{
    public static class AuthConfig
    {
        public static IServiceCollection AddJwtAuthAndPolicies(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    var jwtSettings = configuration.GetSection("Jwt");
                    var keyString = jwtSettings["Key"];
                    if (string.IsNullOrEmpty(keyString))
                    {
                        throw new InvalidOperationException("JWT Key is not configured.");
                    }
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings["Issuer"],
                        ValidAudience = jwtSettings["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString))
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            if (string.IsNullOrEmpty(context.Token))
                            {
                                var token = context.HttpContext.Request.Cookies["token"];
                                if (!string.IsNullOrEmpty(token))
                                {
                                    context.Token = token;
                                }
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("IsPublic", policy =>
                    policy.RequireAssertion(context => true));  // Always allow
                options.AddPolicy("IsAuthenticated", policy =>
                    policy.RequireAuthenticatedUser());  // Any authenticated user
                options.AddPolicy("IsUser", policy =>
                    policy.Requirements.Add(new UserRequirement()));  // Verified user
                options.AddPolicy("IsOwner", policy =>
                    policy.Requirements.Add(new OwnerRequirement()));  // Owner
                options.AddPolicy("IsAdmin", policy =>
                    policy.RequireRole("Admin"));  // Admin only operations
                options.AddPolicy("IsOwnerOrAdmin", policy =>
                    policy.Requirements.Add(new OwnerOrAdminRequirement()));  // Owner or Admin
            });

            return services;
        }
    }
}
