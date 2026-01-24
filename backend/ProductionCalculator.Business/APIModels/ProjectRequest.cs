using System.ComponentModel.DataAnnotations;

namespace ProductionCalculator.Business.APIModels
{
    public class ProjectRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; } = null;
        public bool? IsPublic { get; set; } = null;
        public string? AliasProjectPuid { get; set; }
    }
}
