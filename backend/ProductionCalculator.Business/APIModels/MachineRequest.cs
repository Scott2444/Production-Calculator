using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.APIModels
{
    public class MachineRequest
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required double BaseSpeed { get; set; }
        public required List<string> RecipePuids { get; set; }
    }
}
