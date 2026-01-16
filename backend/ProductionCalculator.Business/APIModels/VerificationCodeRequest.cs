using System.ComponentModel.DataAnnotations;

namespace ProductionCalculator.Business.APIModels
{
    public class VerificationCodeRequest
    {
        [Required]
        public string Code { get; set; } = string.Empty;
    }
}
