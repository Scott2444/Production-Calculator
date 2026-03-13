namespace ProductionCalculator.API.Helpers
{
    public class CookieOptionsHelper
    {
        // isDevelopment is determined if it is running in a container, i.e. local development
        private readonly bool _isDevelopment;
        private readonly int accessTokenExpiryMinutes;
        private readonly int refreshTokenExpiryDays;
        private readonly string? _cookieDomain;
        private readonly SameSiteMode _sameSiteMode;

        public CookieOptionsHelper(IConfiguration configuration)
        {
            _isDevelopment = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true";

            int.TryParse(configuration["Jwt:ExpireMinutes"], out accessTokenExpiryMinutes);
            int.TryParse(configuration["RefreshToken:ExpireDays"], out refreshTokenExpiryDays);

            var sameSiteConfig = configuration["Cookie:SameSite"];
            _sameSiteMode = sameSiteConfig?.ToLowerInvariant() switch
            {
                "none" => SameSiteMode.None,
                "lax" => SameSiteMode.Lax,
                _ => SameSiteMode.Strict
            };

            _cookieDomain = _isDevelopment ? "localhost" : configuration["Cookie:Domain"];
            _sameSiteMode = _isDevelopment ? SameSiteMode.Strict : _sameSiteMode;
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