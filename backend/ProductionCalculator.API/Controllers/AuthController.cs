using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.API.APIModels;
using ProductionCalculator.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;


namespace ProductionCalculator.API.Controllers
{
    [Route("auth")]
    public class AuthController : ApiControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [Authorize(Policy = "IsPublic")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var result = await _authService.Login(req.Username, req.Password);
            return FromServiceResult(result, (u) => new AuthResponse { Token = u });
        }

        [Authorize(Policy = "IsAuthenticated")]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var user = HttpContext.User;
            var result = await _authService.RefreshToken(user);
            return FromServiceResult(result, (u) => new AuthResponse { Token = u });
        }
    }
}
