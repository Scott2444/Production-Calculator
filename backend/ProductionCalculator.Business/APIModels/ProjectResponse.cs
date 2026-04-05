namespace ProductionCalculator.Business.APIModels
{
    public class ProjectResponse
    {
        public required string Puid { get; set; }
        public required string Name { get; set; }
        public required string OwnerUsername { get; set; }
        public string? Description { get; set; }
        public bool IsPublic { get; set; }
        public string? AliasProjectPuid { get; set; }
        public int AliasCount { get; set; }
        public int ProductCount { get; set; }
        public int RecipeCount { get; set; }
        public int MachineCount { get; set; }
        public int ModifierCount { get; set; }
        public int AttributeCount { get; set; }
        public int WorkflowCount { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime UpdatedAt { get; set; }
    }
}
