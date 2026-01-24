namespace ProductionCalculator.Business.APIModels
{
    public class WorkflowRequest
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
    }
}
