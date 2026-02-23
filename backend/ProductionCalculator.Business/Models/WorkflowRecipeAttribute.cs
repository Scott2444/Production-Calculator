namespace ProductionCalculator.Business.Models
{
    public class WorkflowRecipeAttribute
    {
        public required int Workflow_Recipe_Attribute_Id { get; set; }
        public required int Workflow_Id { get; set; }
        public required int Recipe_Id { get; set; }
        public required int Attribute_Id { get; set; }
        public required double Rate { get; set; }
    }
}
