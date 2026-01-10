
using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;


namespace ProductionCalculator.API.Controllers
{
    [Route("auth")]
    public class AuthController : ApiControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly int tokenExpiryMinutes;
        public AuthController(IAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;

            int.TryParse(_configuration["Jwt:ExpireMinutes"], out tokenExpiryMinutes);
        }

        [Authorize(Policy = "IsPublic")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var (result, token) = await _authService.Login(req.Username, req.Password);
            // Set token cookie
            if (token != null)
            {
                Response.Cookies.Append(
                    "token",
                    token,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddMinutes(tokenExpiryMinutes)
                    }
                );
            }
            return FromServiceResult(result, u => u);
        }

        [Authorize(Policy = "IsAuthenticated")]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var user = HttpContext.User;
            var (result, token) = await _authService.RefreshToken(user);
            // Set token cookie
            if (token != null)
            {
                Response.Cookies.Append(
                    "token",
                    token,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddMinutes(tokenExpiryMinutes)
                    }
                );
            }
            return FromServiceResult(result, u => u);
        }
    }
}
