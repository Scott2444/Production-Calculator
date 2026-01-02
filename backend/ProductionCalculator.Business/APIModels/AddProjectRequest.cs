using System.ComponentModel.DataAnnotations;

namespace ProductionCalculator.Business.APIModels
{
    public class AddProjectRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; } = null;
    }
}
