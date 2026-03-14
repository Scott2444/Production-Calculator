namespace ProductionCalculator.Business.APIModels
{
    public class ModifierAttributeResponse
    {
        public required string Puid { get; set; }
        public required double FlatBonus { get; set; }
        public required double PercentBonus { get; set; }
        public required double MultiplicativeBonus { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime UpdatedAt { get; set; }
    }
}
