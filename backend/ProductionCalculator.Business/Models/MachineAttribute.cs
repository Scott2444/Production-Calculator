namespace ProductionCalculator.Business.Models
{
    public class MachineAttribute
    {
        public required int Machine_Attribute_Id { get; set; }
        public required int Machine_Id { get; set; }
        public required int Attribute_Id { get; set; }
        public required double Rate { get; set; }
        public required int Version { get; set; }
        public required DateTime Created_At { get; set; }
        public required DateTime Last_Updated { get; set; }
    }
}
