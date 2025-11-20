using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace ProductionCalculator.Business.Helpers
{
    public class PasswordHelper
    {
        // Creates a new hashed password
        public static string HashPassword(string password)
        {
            // From ASP.NET Docs:
            // https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/password-hashing?view=aspnetcore-10.0

            // Generate a 128-bit salt using a sequence of
            // cryptographically strong random bytes.
            byte[] salt = RandomNumberGenerator.GetBytes(128 / 8); // divide by 8 to convert bits to bytes

            // derive a 256-bit subkey (use HMACSHA256 with 100,000 iterations)
            byte[] hashBytes = KeyDerivation.Pbkdf2(
                password: password!,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8);

            // Format: {iterations}.{salt}.{hash}  (all Base64 encoded)
            return $"100000.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hashBytes)}";
        }
        // Verifies a password against a stored hash
        public static bool VerifyPassword(string password, string storedHash)
        {
            string[] parts = storedHash.Split('.');
            if (parts.Length != 3 || !int.TryParse(parts[0], out int iterations))
                throw new FormatException("Invalid stored hash format");

            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] expectedHash = Convert.FromBase64String(parts[2]);

            byte[] actualHash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: iterations,
                numBytesRequested: 256 / 8);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
    }
}
