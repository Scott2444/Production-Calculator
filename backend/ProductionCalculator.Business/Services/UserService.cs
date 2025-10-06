using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;

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
            if (existingUsername != null) return ServiceResult<User>.Fail(ServiceStatus.Conflict409);
            var existingEmail = await _repo.GetByEmail(email);
            if (existingEmail != null) return ServiceResult<User>.Fail(ServiceStatus.Conflict409);

            // Hash password with salt using PBKDF2
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[16];
            rng.GetBytes(salt);

            var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, 100_000, HashAlgorithmName.SHA256, 32);

            var stored = Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);

            var user = new User
            {
                User_Id = 0,
                Username = username,
                Email = email,
                Password_Hash = stored,
                Created_At = DateTime.UtcNow
            };

            await _repo.AddUser(user);
            return ServiceResult<User>.SuccessResult(user, ServiceStatus.Created201);
        }

        public async Task<ServiceResult<User>> GetUserById(int id)
        {
            if (id <= 0) return ServiceResult<User>.Fail(ServiceStatus.BadRequest400);

            var user = await _repo.GetById(id);
            if (user == null) return ServiceResult<User>.Fail(ServiceStatus.NotFound404);

            return ServiceResult<User>.SuccessResult(user, ServiceStatus.Ok200);
        }
    }
}
