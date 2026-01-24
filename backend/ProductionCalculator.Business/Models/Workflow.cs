namespace ProductionCalculator.Business.Models
{
    public class Workflow
    {
        public required int Workflow_Id { get; set; }
        public required int Project_Id { get; set; }
        public required string Puid { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required DateTime Created_At { get; set; }
        public required DateTime Last_Updated { get; set; }
    }
}
