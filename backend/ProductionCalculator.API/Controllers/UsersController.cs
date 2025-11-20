using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.API.APIModels;

namespace ProductionCalculator.API.Controllers
{
    [Route("[controller]")]
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
            var result = await _service.RegisterAsync(req.Username, req.Email, req.Password);

            return FromServiceResult(result, (u) => new UserResponse { UserId = u.User_Id, Username = u.Username, Email = u.Email, Role_Id = u.Role_Id, CreatedAt = u.Created_At, UpdatedAt = u.Last_Updated });
        }

        [HttpGet("{user_id:int}")]
        public async Task<IActionResult> GetByUsername(int user_id)
        {
            var result = await _service.GetUserById(user_id);
            return FromServiceResult(result, u => new UserResponse { UserId = u.User_Id, Username = u.Username, Email = u.Email, Role_Id = u.Role_Id, CreatedAt = u.Created_At, UpdatedAt = u.Last_Updated });
        }

        [HttpDelete("{user_id:int}")]
        public async Task<IActionResult> DeleteByUsername(int user_id)
        {
            var result = await _service.DeleteUserById(user_id);
            return FromServiceResult(result);
        }
    }
}
