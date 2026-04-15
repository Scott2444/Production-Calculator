using System.ComponentModel.DataAnnotations;

namespace ProductionCalculator.Business.APIModels
{
    public class SetRegistrationEnabledRequest
    {
        [Required]
        public bool? IsEnabled { get; set; }
    }
}