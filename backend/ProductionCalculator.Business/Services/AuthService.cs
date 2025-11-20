using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Helpers;

namespace ProductionCalculator.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _repo;
        private readonly JwtHelper _jwtHelper;
        public AuthService(IUserRepository repo, JwtHelper jwtHelper)
        {
            _repo = repo;
            _jwtHelper = jwtHelper;
        }

        public async Task<ServiceResult<string>> Login(string username, string password)
        {
            var userResult = await _repo.GetByUsername(username);
            if (userResult == null)
                return ServiceResult<string>.Fail(ServiceStatus.Unauthorized401, "Invalid username or password.");

            var storedHash = await _repo.GetPasswordHash(userResult.User_Id);
            if (!PasswordHelper.VerifyPassword(password, storedHash))
                return ServiceResult<string>.Fail(ServiceStatus.Unauthorized401, "Invalid username or password.");

            var token = _jwtHelper.GenerateToken(username);
            return ServiceResult<string>.SuccessResult(token);
        }
        public async Task<ServiceResult<string>> RefreshToken(string token)
        {
            var principal = _jwtHelper.ValidateToken(token);
            if (principal == null)
                return ServiceResult<string>.Fail(ServiceStatus.Unauthorized401, "Invalid or expired token.");

            var username = principal.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return ServiceResult<string>.Fail(ServiceStatus.Unauthorized401, "Invalid token claims.");

            var newToken = _jwtHelper.GenerateToken(username);
            return ServiceResult<string>.SuccessResult(newToken);
        }
    }
}
