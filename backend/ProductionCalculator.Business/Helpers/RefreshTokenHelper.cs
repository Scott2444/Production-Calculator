using Microsoft.Extensions.Configuration;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Helpers
{
    public class RefreshTokenHelper
    {
        private readonly IConfiguration _config;
        public RefreshTokenHelper(IConfiguration config)
        {
            _config = config;
        }

        public RefreshToken AssembleRefreshToken(int userId, int size = 128)
        {
            var refreshTokenSettings = _config.GetSection("RefreshToken");
            var expireDays = refreshTokenSettings["ExpireDays"];
            if (expireDays == null)
            {
                throw new Exception("Refresh token expiration days not configured.");
            }
            var expireDaysDouble = double.Parse(expireDays);

            var refreshToken = new RefreshToken
            {
                Token_Id = Guid.NewGuid(),
                User_Id = userId,
                Token = GenerateRefreshToken(size),
                Expires_At = DateTime.UtcNow.AddDays(expireDaysDouble),
                Created_At = DateTime.UtcNow
            };
            return refreshToken;
        }

        private string GenerateRefreshToken(int size)
        {
            // Generates a cryptographically secure random refresh token string (base64 encoded)
            var randomBytes = new byte[size];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            // Use base64url encoding (no +, /, =)
            string token = Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
            return token;
        }
    }
}
