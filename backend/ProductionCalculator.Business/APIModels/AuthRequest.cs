using System.ComponentModel.DataAnnotations;

namespace ProductionCalculator.Business.APIModels
{

    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class RefreshRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }
}