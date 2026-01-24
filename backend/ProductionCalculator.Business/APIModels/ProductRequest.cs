using System.ComponentModel.DataAnnotations;

namespace ProductionCalculator.Business.APIModels
{
    public class ProductRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; } = null;
    }
}
