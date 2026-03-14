namespace ProductionCalculator.Business.APIModels
{
    public class RecipeRequest
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required double BaseCraftingTime { get; set; }
        public required List<RecipeProductExchange> Inputs { get; set; }
        public required List<RecipeProductExchange> Outputs { get; set; }
        public List<AttributeRateRequest> Attributes { get; set; } = [];
    }
}
