using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.APIModels;
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
        [HttpGet("{puid}")]
        public async Task<IActionResult> GetBypuid(string puid)
        {
            var result = await _service.GetUserBypuid(puid);
            return FromServiceResult(result, u => new UserResponse { Username = u.Username, Email = u.Email, Puid = u.Puid, CreatedAt = u.Created_At, UpdatedAt = u.Last_Updated });
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpGet("{puid}/projects")]
        public async Task<IActionResult> GetProjectsByUserPuid(string puid, [FromServices] IProjectService projectService)
        {
            var result = await projectService.GetProjectsByUserPuid(puid);
            return FromServiceResult(result, projects => projects.Select(p => new ProjectResponse { Puid = p.Puid, Name = p.Name, Description = p.Description, CreatedAt = p.Created_At, UpdatedAt = p.Last_Updated }).ToList());
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpDelete("{puid}")]
        public async Task<IActionResult> DeleteBypuid(string puid)
        {
            var result = await _service.DeleteUserById(puid);
            return FromServiceResult(result);
        }
    }
}
