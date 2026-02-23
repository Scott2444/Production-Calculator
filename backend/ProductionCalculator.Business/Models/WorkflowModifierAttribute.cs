namespace ProductionCalculator.Business.Models
{
    public class WorkflowModifierAttribute
    {
        public required int Workflow_Modifier_Attribute_Id { get; set; }
        public required int Workflow_Id { get; set; }
        public required int Modifier_Id { get; set; }
        public required int Attribute_Id { get; set; }
        public required double Flat_Bonus { get; set; }
        public required double Percent_Bonus { get; set; }
        public required double Multiplicative_Bonus { get; set; }
    }
}
