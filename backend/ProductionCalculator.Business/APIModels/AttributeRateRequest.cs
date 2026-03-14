namespace ProductionCalculator.Business.APIModels
{
    public class AttributeRateRequest
    {
        public required string Puid { get; set; }
        public required double Rate { get; set; }
    }
}
