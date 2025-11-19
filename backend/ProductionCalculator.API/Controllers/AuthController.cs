using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ProductionCalculator.API.Helpers;
using ProductionCalculator.API.APIModels;
using ProductionCalculator.Business.Interfaces;
using System.Threading.Tasks;

namespace ProductionCalculator.API.Controllers
{
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly JwtHelper _jwtHelper;

        public AuthController(IUserService userService, IConfiguration config)
        {
            _userService = userService;
            _jwtHelper = new JwtHelper(config);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var userResult = await _userService.GetUserByUsername(req.Username);
            if (!userResult.Success || userResult.Data == null)
                return Unauthorized(new { message = "Invalid username or password." });

            // TODO: Validate password hash
            // For now, assume password is valid
            var token = _jwtHelper.GenerateToken(req.Username);
            return Ok(new { token });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
        {
            var principal = _jwtHelper.ValidateToken(req.Token);
            if (principal == null)
                return Unauthorized(new { message = "Invalid or expired token." });

            var username = principal.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized(new { message = "Invalid token claims." });

            var newToken = _jwtHelper.GenerateToken(username);
            return Ok(new { token = newToken });
        }
    }
}
