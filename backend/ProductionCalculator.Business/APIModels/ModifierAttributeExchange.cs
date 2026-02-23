namespace ProductionCalculator.Business.APIModels
{
    public class ModifierAttributeExchange
    {
        public required string Puid { get; set; }
        public required double FlatBonus { get; set; }
        public required double PercentBonus { get; set; }
        public required double MultiplicativeBonus { get; set; }
    }
}
