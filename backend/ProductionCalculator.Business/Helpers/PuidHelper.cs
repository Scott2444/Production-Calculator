using System.Security.Cryptography;
using System.Text;

namespace ProductionCalculator.Business.Helpers
{
    /// <summary>
    /// Helper class for generating unique public IDs (PUIDs).
    /// </summary>
    public static class PuidHelper
    {
        // Base58 alphabet (no 0, O, I, l, +, /) to be url friendly
        private const string Base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        private const int PuidLength = 10;

        /// <summary>
        /// Generates a unique public ID for a resource, checking for collisions using the provided async exists function.
        /// </summary>
        /// <param name="exists">A function that checks if a PUID already exists for the resource.</param>
        /// <returns>A unique base58 public ID.</returns>
        public static async Task<string> GenerateUniquePuidAsync(Func<string, Task<bool>> exists)
        {
            string puid;
            do
            {
                puid = GeneratePuid();
            } while (await exists(puid));
            return puid;
        }

        /// <summary>
        /// Generates a random base58 public ID.
        /// </summary>
        /// <returns>Base58 string of length 10.</returns>
        public static string GeneratePuid()
        {
            var bytes = new byte[PuidLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            var sb = new StringBuilder(PuidLength);
            foreach (var b in bytes)
            {
                sb.Append(Base58Alphabet[b % Base58Alphabet.Length]);
            }
            return sb.ToString();
        }
    }
}