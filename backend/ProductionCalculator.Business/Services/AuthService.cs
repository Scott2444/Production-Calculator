using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Helpers;
using System.Security.Claims;

namespace ProductionCalculator.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _repo;
        private readonly IRoleRepository _roleRepo;
        private readonly JwtHelper _jwtHelper;
        public AuthService(IUserRepository repo, IRoleRepository roleRepo, JwtHelper jwtHelper)
        {
            _repo = repo;
            _roleRepo = roleRepo;
            _jwtHelper = jwtHelper;
        }

        public async Task<ServiceResult<string>> Login(string username, string password)
        {
            // Validate user credentials
            var userResult = await _repo.GetByUsername(username);
            if (userResult == null)
                return ServiceResult<string>.Fail(ServiceStatus.Unauthorized401, "Invalid username or password.");

            var storedHash = await _repo.GetPasswordHash(userResult.User_Id);
            if (!PasswordHelper.VerifyPassword(password, storedHash))
                return ServiceResult<string>.Fail(ServiceStatus.Unauthorized401, "Invalid username or password.");
            
            // Get claims for JWT
            var userId = userResult.User_Id;
            var role = await _roleRepo.GetRole(userResult.Role_Id);
            if (role == null)
                return ServiceResult<string>.Fail(ServiceStatus.InternalServerError500, $"User id {userResult.Role_Id} not found.");

            // Generate JWT
            var token = _jwtHelper.GenerateToken(userId, role.Role_Name);
            return ServiceResult<string>.SuccessResult(token);
        }
        public async Task<ServiceResult<string>> RefreshToken(string token)
        {
            var principal = _jwtHelper.ValidateToken(token);
            if (principal == null)
                return ServiceResult<string>.Fail(ServiceStatus.Unauthorized401, "Invalid or expired token.");

            var userId = Convert.ToInt32(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var roleName = principal.FindFirst(ClaimTypes.Role)?.Value;
            if (userId == 0 || string.IsNullOrEmpty(roleName))
                return ServiceResult<string>.Fail(ServiceStatus.Unauthorized401, "Invalid token claims.");

            var newToken = _jwtHelper.GenerateToken(userId, roleName);
            return ServiceResult<string>.SuccessResult(newToken);
        }
    }
}
