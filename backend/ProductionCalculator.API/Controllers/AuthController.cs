using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ProductionCalculator.Business.Helpers;
using ProductionCalculator.API.APIModels;
using ProductionCalculator.Business.Interfaces;
using System.Threading.Tasks;

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

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var result = await _authService.Login(req.Username, req.Password);
            return FromServiceResult(result, (u) => new AuthResponse { Token = u });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
        {
            var result = await _authService.RefreshToken(req.Token);
            return FromServiceResult(result, (u) => new AuthResponse { Token = u });
        }
    }
}
