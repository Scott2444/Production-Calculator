using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.API.APIModels;
using Microsoft.AspNetCore.Authorization;

namespace ProductionCalculator.API.Controllers
{
    [Route("users")]
    public class UsersController : ApiControllerBase
    {
        private readonly IUserService _service;

        public UsersController(IUserService service)
        {
            _service = service;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest req)
        {
            var result = await _service.Register(req.Username, req.Email, req.Password);

            return FromServiceResult(result, (u) => new UserResponse { Username = u.Username, Email = u.Email, Puid = u.Puid, CreatedAt = u.Created_At, UpdatedAt = u.Last_Updated });
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpGet("{pubId}")]
        public async Task<IActionResult> GetByPubId(string pubId)
        {
            var result = await _service.GetUserByPubId(pubId);
            return FromServiceResult(result, u => new UserResponse { Username = u.Username, Email = u.Email, Puid = u.Puid, CreatedAt = u.Created_At, UpdatedAt = u.Last_Updated });
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpDelete("{pubId}")]
        public async Task<IActionResult> DeleteByPubId(string pubId)
        {
            var result = await _service.DeleteUserById(pubId);
            return FromServiceResult(result);
        }
    }
}
