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

        public async Task<ServiceResult<User>> Register(string username, string email, string password)
        {
            // basic checks
            email = email.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(username)) return ServiceResult<User>.Fail(ServiceStatus.BadRequest400);
            if (string.IsNullOrWhiteSpace(email)) return ServiceResult<User>.Fail(ServiceStatus.BadRequest400);
            if (string.IsNullOrWhiteSpace(password)) return ServiceResult<User>.Fail(ServiceStatus.BadRequest400);
            if (username.Length < 3) return ServiceResult<User>.Fail(ServiceStatus.BadRequest400, "Username must be at least 3 characters long.");
            if (username.Length > 20) return ServiceResult<User>.Fail(ServiceStatus.BadRequest400, "Username must be no more than 20 characters long.");
            if (username.Any(c => !char.IsLetterOrDigit(c) && c != '_' && c != '-'))
                return ServiceResult<User>.Fail(ServiceStatus.BadRequest400, "Username can only contain letters, numbers, underscores, and hyphens.");
            if (password.Length < 8) return ServiceResult<User>.Fail(ServiceStatus.BadRequest400, "Password must be at least 8 characters long.");
            if (password.Length > 32) return ServiceResult<User>.Fail(ServiceStatus.BadRequest400, "Password must be no more than 32 characters long.");

            var existingUsername = await _repo.GetByUsername(username);
            if (existingUsername != null) return ServiceResult<User>.Fail(ServiceStatus.Conflict409, $"Username {username} already exists.");
            var existingEmail = await _repo.GetByEmail(email);
            if (existingEmail != null) return ServiceResult<User>.Fail(ServiceStatus.Conflict409, $"Email {email} already exists.");

            var passwordHash = PasswordHelper.HashPassword(password);

            var puid = await PuidHelper.GenerateUniquePuidAsync(_repo.PuidExists);

            var user = new User
            {
                User_Id = 0,
                Username = username,
                Email = email,
                Password_Hash = passwordHash,
                Role_Id = 1, // Default role / unverified
                Puid = puid,
                Created_At = DateTime.UtcNow,
                Last_Updated = DateTime.UtcNow
            };

            await _repo.AddUser(user);
            return ServiceResult<User>.SuccessResult(user, ServiceStatus.Created201);
        }

        public async Task<ServiceResult> ValidateNewUser(string username, string email)
        {
            email = email.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(username)) return ServiceResult.Fail(ServiceStatus.BadRequest400);
            if (string.IsNullOrWhiteSpace(email)) return ServiceResult.Fail(ServiceStatus.BadRequest400);
            var existingUsername = await _repo.GetByUsername(username);
            if (existingUsername != null) return ServiceResult.Fail(ServiceStatus.Conflict409, $"Username already exists.");
            var existingEmail = await _repo.GetByEmail(email);
            if (existingEmail != null) return ServiceResult.Fail(ServiceStatus.Conflict409, $"Email already exists.");

            return ServiceResult.SuccessResult(ServiceStatus.Ok200);
        }

        public async Task<ServiceResult<User>> GetUserByPuid(string puid)
        {
            if (string.IsNullOrWhiteSpace(puid)) return ServiceResult<User>.Fail(ServiceStatus.BadRequest400);

            var user = await _repo.GetByPuid(puid);
            if (user == null) return ServiceResult<User>.Fail(ServiceStatus.NotFound404, $"User with PUID {puid} not found.");

            return ServiceResult<User>.SuccessResult(user, ServiceStatus.Ok200);
        }

        public async Task<ServiceResult<User>> GetUserByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return ServiceResult<User>.Fail(ServiceStatus.BadRequest400);

            var user = await _repo.GetByUsername(username);
            if (user == null) return ServiceResult<User>.Fail(ServiceStatus.NotFound404, $"{username} not found.");

            return ServiceResult<User>.SuccessResult(user, ServiceStatus.Ok200);
        }
        public async Task<ServiceResult> DeleteUserById(string puid)
        {
            if (string.IsNullOrWhiteSpace(puid))
                return ServiceResult.Fail(ServiceStatus.BadRequest400);

            var user = await _repo.GetByPuid(puid);
            if (user == null)
                return ServiceResult.Fail(ServiceStatus.NotFound404, $"User with PUID {puid} not found.");

            var deleted = await _repo.DeleteUser(user.User_Id);
            if (!deleted)
                return ServiceResult.Fail(ServiceStatus.InternalServerError500);

            return ServiceResult.SuccessResult(ServiceStatus.NoContent204);
        }
    }
}
