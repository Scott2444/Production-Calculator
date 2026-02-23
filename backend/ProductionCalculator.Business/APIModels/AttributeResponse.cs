namespace ProductionCalculator.Business.APIModels
{
    public class AttributeResponse
    {
        public required string Puid { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? Unit { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime UpdatedAt { get; set; }
    }
}
