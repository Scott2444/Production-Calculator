namespace ProductionCalculator.Business.APIModels
{
    public class ModifierResponse
    {
        public required string Puid { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required double FlatBonus { get; set; }
        public required double PercentBonus { get; set; }
        public required double MultiplicativeBonus { get; set; }
        public required double InputPercent { get; set; }
        public required double OutputPercent { get; set; }
        public required List<ModifierAttributeResponse> Attributes { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime UpdatedAt { get; set; }
    }
}
