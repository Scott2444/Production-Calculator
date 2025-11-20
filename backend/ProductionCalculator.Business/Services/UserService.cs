using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Helpers;

namespace ProductionCalculator.Business.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;

        public UserService(IUserRepository repo)
        {
            _repo = repo;
        }

        public async Task<ServiceResult<User>> RegisterAsync(string username, string email, string password)
        {
            // basic checks
            if (string.IsNullOrWhiteSpace(username)) return ServiceResult<User>.Fail(ServiceStatus.BadRequest400);
            if (string.IsNullOrWhiteSpace(email)) return ServiceResult<User>.Fail(ServiceStatus.BadRequest400);
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8) return ServiceResult<User>.Fail(ServiceStatus.BadRequest400);

            var existingUsername = await _repo.GetByUsername(username);
            if (existingUsername != null) return ServiceResult<User>.Fail(ServiceStatus.Conflict409, $"Username {username} already exists.");
            var existingEmail = await _repo.GetByEmail(email);
            if (existingEmail != null) return ServiceResult<User>.Fail(ServiceStatus.Conflict409, $"Email {email} already exists.");

            var passwordHash = PasswordHelper.HashPassword(password);

            var user = new User
            {
                User_Id = 0,
                Username = username,
                Email = email,
                Password_Hash = passwordHash,
                Created_At = DateTime.UtcNow,
                Last_Updated = DateTime.UtcNow
            };

            await _repo.AddUser(user);
            return ServiceResult<User>.SuccessResult(user, ServiceStatus.Created201);
        }

        public async Task<ServiceResult<User>> GetUserById(int id)
        {
            if (id <= 0) return ServiceResult<User>.Fail(ServiceStatus.BadRequest400);

            var user = await _repo.GetById(id);
            if (user == null) return ServiceResult<User>.Fail(ServiceStatus.NotFound404, $"User with ID {id} not found.");

            return ServiceResult<User>.SuccessResult(user, ServiceStatus.Ok200);
        }

        public async Task<ServiceResult<User>> GetUserByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return ServiceResult<User>.Fail(ServiceStatus.BadRequest400);

            var user = await _repo.GetByUsername(username);
            if (user == null) return ServiceResult<User>.Fail(ServiceStatus.NotFound404, $"{username} not found.");

            return ServiceResult<User>.SuccessResult(user, ServiceStatus.Ok200);
        }
        public async Task<ServiceResult> DeleteUserByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return ServiceResult.Fail(ServiceStatus.BadRequest400);

            var user = await _repo.GetByUsername(username);
            if (user == null)
                return ServiceResult.Fail(ServiceStatus.NotFound404, $"{username} not found.");

            var deleted = await _repo.DeleteUser(user.User_Id);
            if (!deleted)
                return ServiceResult.Fail(ServiceStatus.InternalServerError500);

            return ServiceResult.SuccessResult(ServiceStatus.NoContent204);
        }
    }
}
