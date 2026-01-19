using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.APIModels;
using Microsoft.AspNetCore.Authorization;

namespace ProductionCalculator.API.Controllers
{
    [Route("api/users")]
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
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateNewUser([FromBody] ValidateNewUserRequest req)
        {
            var result = await _service.ValidateNewUser(req.Username, req.Email);
            return FromServiceResult(result);
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpGet("{userPuid}")]
        public async Task<IActionResult> GetByPuid(string userPuid)
        {
            var serviceResult = await _service.GetUserByPuid(userPuid);
            return FromServiceResult(serviceResult, tuple => new UserResponse
            {
                Username = tuple.Item1.Username,
                Email = tuple.Item1.Email,
                Puid = tuple.Item1.Puid,
                CreatedAt = tuple.Item1.Created_At,
                UpdatedAt = tuple.Item1.Last_Updated,
                IsVerified = tuple.Item2
            });
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpGet("{userPuid}/projects")]
        public async Task<IActionResult> GetProjectsByUserPuid(string userPuid, [FromServices] IProjectService projectService)
        {
            var result = await projectService.GetProjectsByUserPuid(userPuid);
            return FromServiceResult(result, projects => projects.Select(p => new ProjectResponse { Puid = p.Puid, Name = p.Name, Description = p.Description, IsPublic = p.Is_Public, AliasProjectPuid = p.Alias_Project_Puid, CreatedAt = p.Created_At, UpdatedAt = p.Last_Updated }).ToList());
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpDelete("{userPuid}")]
        public async Task<IActionResult> DeleteBypuid(string userPuid)
        {
            var result = await _service.DeleteUserById(userPuid);
            return FromServiceResult(result);
        }
    }
}
