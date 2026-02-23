using System.ComponentModel.DataAnnotations;

namespace ProductionCalculator.Business.APIModels
{
    public class AttributeRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Unit { get; set; }
    }
}
