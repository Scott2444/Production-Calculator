namespace ProductionCalculator.API.Helpers
{
    public class CookieOptionsHelper
    {
        private readonly bool _isDevelopment;
        private readonly int accessTokenExpiryMinutes;
        private readonly int refreshTokenExpiryDays;
        private readonly string? _cookieDomain;
        private readonly SameSiteMode _sameSiteMode;

        public CookieOptionsHelper(IConfiguration configuration)
        {
            _isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

            int.TryParse(configuration["Jwt:ExpireMinutes"], out accessTokenExpiryMinutes);
            int.TryParse(configuration["RefreshToken:ExpireDays"], out refreshTokenExpiryDays);

            _cookieDomain = configuration["Cookie:Domain"];

            var sameSiteConfig = configuration["Cookie:SameSite"];
            _sameSiteMode = sameSiteConfig?.ToLowerInvariant() switch
            {
                "none" => SameSiteMode.None,
                "lax" => SameSiteMode.Lax,
                _ => SameSiteMode.Strict
            };
        }

        private CookieOptions BuildCookieOptions(bool httpOnly, DateTimeOffset expires)
        {
            var options = new CookieOptions
            {
                HttpOnly = httpOnly,
                Secure = !_isDevelopment,
                SameSite = _sameSiteMode,
                Expires = expires,
                Path = "/"
            };

            if (!string.IsNullOrWhiteSpace(_cookieDomain))
            {
                options.Domain = _cookieDomain;
            }

            return options;
        }

        public CookieOptions BuildAccessCookieOptions()
        {
            return BuildCookieOptions(
                httpOnly: true,
                expires: DateTimeOffset.UtcNow.AddMinutes(accessTokenExpiryMinutes)
            );
        }
        public CookieOptions BuildRefreshCookieOptions()
        {
            return BuildCookieOptions(
                httpOnly: true,
                expires: DateTimeOffset.UtcNow.AddDays(refreshTokenExpiryDays)
            );
        }
        public CookieOptions BuildUserIdCookieOptions()
        {
            return BuildCookieOptions(
                httpOnly: false,
                expires: DateTimeOffset.UtcNow.AddDays(refreshTokenExpiryDays)
            );
        }
    }
}