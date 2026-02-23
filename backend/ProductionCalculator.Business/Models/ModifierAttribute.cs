namespace ProductionCalculator.Business.Models
{
    public class ModifierAttribute
    {
        public required int Modifier_Attribute_Id { get; set; }
        public required int Modifier_Id { get; set; }
        public required int Attribute_Id { get; set; }
        public required double Flat_Bonus { get; set; }
        public required double Percent_Bonus { get; set; }
        public required double Multiplicative_Bonus { get; set; }
        public required int Version { get; set; }
        public required DateTime Created_At { get; set; }
        public required DateTime Last_Updated { get; set; }
    }
}
