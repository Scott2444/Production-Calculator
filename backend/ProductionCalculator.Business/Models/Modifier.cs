namespace ProductionCalculator.Business.Models
{
    public class Modifier
    {
        public required int Modifier_Id { get; set; }
        public required int Project_Id { get; set; }
        public required string Puid { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required double Flat_Speed_Bonus { get; set; }
        public required double Additive_Percent_Bonus { get; set; }
        public required double Multiplicative_Modifiers { get; set; }
        public required int Version { get; set; }
        public required DateTime Created_At { get; set; }
        public required DateTime Last_Updated { get; set; }
    }
}
