namespace ProductionCalculator.API.Helpers
{
    public class CookieOptionsHelper
    {
        private readonly bool _isDevelopment;
        private readonly int accessTokenExpiryMinutes;
        private readonly int refreshTokenExpiryDays;

        public CookieOptionsHelper(IConfiguration configuration)
        {
            _isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

            int.TryParse(configuration["Jwt:ExpireMinutes"], out accessTokenExpiryMinutes);
            int.TryParse(configuration["RefreshToken:ExpireDays"], out refreshTokenExpiryDays);
        }

        public CookieOptions BuildAccessCookieOptions()
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = !_isDevelopment,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(accessTokenExpiryMinutes),
                Path = "/"
            };
        }
        public CookieOptions BuildRefreshCookieOptions()
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = !_isDevelopment,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(refreshTokenExpiryDays),
                Path = "/"
            };
        }
        public CookieOptions BuildUserIdCookieOptions()
        {
            return new CookieOptions
            {
                HttpOnly = false,
                Secure = !_isDevelopment,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(refreshTokenExpiryDays),
                Path = "/"
            };
        }
    }
}