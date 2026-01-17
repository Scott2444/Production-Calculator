namespace ProductionCalculator.Business.APIModels
{
    public class ModifierRequest
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required double FlatSpeedBonus { get; set; }
        public required double AdditivePercentBonus { get; set; }
        public required double MultiplicativeModifier { get; set; }
    }
}
