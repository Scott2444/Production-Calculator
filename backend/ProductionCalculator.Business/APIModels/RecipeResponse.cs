using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.APIModels
{
    public class RecipeResponse
    {
        public required string Puid { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required double BaseCraftingTime { get; set; }
        public required List<RecipeProductExchange> Inputs { get; set; }
        public required List<RecipeProductExchange> Outputs { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime UpdatedAt { get; set; }
    }
}
