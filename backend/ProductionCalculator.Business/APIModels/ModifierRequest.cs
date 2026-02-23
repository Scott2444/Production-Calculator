namespace ProductionCalculator.Business.APIModels
{
    public class ModifierRequest
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required double FlatBonus { get; set; }
        public required double PercentBonus { get; set; }
        public required double MultiplicativeBonus { get; set; }
        public double InputPercent { get; set; } = 1.0;
        public double OutputPercent { get; set; } = 1.0;
        public List<ModifierAttributeExchange> Attributes { get; set; } = [];
    }
}
