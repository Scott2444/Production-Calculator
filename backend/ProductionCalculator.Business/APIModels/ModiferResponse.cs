namespace ProductionCalculator.Business.APIModels
{
    public class ModifierResponse
    {
        public required string Puid { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required double FlatSpeedBonus { get; set; }
        public required double AdditivePercentBonus { get; set; }
        public required double MultiplicativeModifier { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime UpdatedAt { get; set; }
    }
}
