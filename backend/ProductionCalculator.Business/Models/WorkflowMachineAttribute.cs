namespace ProductionCalculator.Business.Models
{
    public class WorkflowMachineAttribute
    {
        public required int Workflow_Machine_Attribute_Id { get; set; }
        public required int Workflow_Id { get; set; }
        public required int Machine_Id { get; set; }
        public required int Attribute_Id { get; set; }
        public required double Rate { get; set; }
    }
}
