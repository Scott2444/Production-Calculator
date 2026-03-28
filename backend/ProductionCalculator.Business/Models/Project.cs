namespace ProductionCalculator.Business.Models
{
    public class Project
    {
        public required int Project_Id { get; set; }
        public required int User_Id { get; set; }
        public required string Puid { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public bool Is_Public { get; set; }
        public string? Alias_Project_Puid { get; set; }
        public int Alias_Count { get; set; } = 0;
        public required DateTime Created_At { get; set; }
        public required DateTime Last_Updated { get; set; }
    }
}
