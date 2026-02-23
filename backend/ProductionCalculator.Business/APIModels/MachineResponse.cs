using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.APIModels
{
    public class MachineResponse
    {
        public required string Puid { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required double BaseSpeed { get; set; }
        public required List<string> RecipePuids { get; set; }
        public required List<AttributeRateExchange> Attributes { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime UpdatedAt { get; set; }
    }
}
