using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.API.Helpers;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;


namespace ProductionCalculator.API.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : ApiControllerBase
    {
        private readonly IAuthService _authService;
        private CookieOptionsHelper _cookieOptionsHelper;
        public AuthController(IAuthService authService, CookieOptionsHelper cookieOptionsHelper)
        {
            _authService = authService;
            _cookieOptionsHelper = cookieOptionsHelper;
        }

        [Authorize(Policy = "None")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var (result, access_token, refresh_token) = await _authService.Login(req.Username, req.Password);
            // Set token cookies
            if (access_token != null && refresh_token != null)
            {
                Response.Cookies.Append(
                    "access_token",
                    access_token,
                    _cookieOptionsHelper.BuildAccessCookieOptions()
                );
                Response.Cookies.Append(
                    "refresh_token",
                    refresh_token.Token,
                    _cookieOptionsHelper.BuildRefreshCookieOptions()
                );
            }
            return FromServiceResult(result, u => u);
        }

        [Authorize(Policy = "None")]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies["refresh_token"];
            var (result, access_token) = await _authService.RefreshToken(refreshToken);
            // Set token cookie
            if (access_token != null)
            {
                Response.Cookies.Append(
                    "access_token",
                    access_token,
                    _cookieOptionsHelper.BuildAccessCookieOptions()
                );
            }
            return FromServiceResult(result, u => u);
        }

        [Authorize(Policy = "IsAuthenticated")]
        [HttpPost("request-code")]
        public async Task<IActionResult> RequestVerificationCode()
        {
            var result = await _authService.RequestVerificationCode();
            return FromServiceResult(result);
        }

        [Authorize(Policy = "IsAuthenticated")]
        [HttpPost("verify-code")]
        public async Task<IActionResult> VerifyCode([FromBody] VerificationCodeRequest req)
        {
            var result = await _authService.VerifyCode(req.Code);
            return FromServiceResult(result);
        }
    }
}
