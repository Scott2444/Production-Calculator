namespace ProductionCalculator.Business.Models
{
    public class WorkflowModifierAttribute
    {
        public required int Workflow_Modifier_Attribute_Id { get; set; }
        public required int Workflow_Node_Id { get; set; }
        public required int Workflow_Node_Modifier_Id { get; set; }
        public int? Modifier_Id { get; set; }
        public required int Attribute_Id { get; set; }
        public required double Flat_Bonus { get; set; }
        public required double Percent_Bonus { get; set; }
        public required double Multiplicative_Bonus { get; set; }

        public bool ValueEquals(WorkflowModifierAttribute other)
        {
            if (other == null) return false;
            return Workflow_Modifier_Attribute_Id == other.Workflow_Modifier_Attribute_Id
                && Workflow_Node_Id == other.Workflow_Node_Id
                && Workflow_Node_Modifier_Id == other.Workflow_Node_Modifier_Id
                && Modifier_Id == other.Modifier_Id
                && Attribute_Id == other.Attribute_Id
                && Flat_Bonus == other.Flat_Bonus
                && Percent_Bonus == other.Percent_Bonus
                && Multiplicative_Bonus == other.Multiplicative_Bonus;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Workflow_Modifier_Attribute_Id,
                Workflow_Node_Id,
                Workflow_Node_Modifier_Id,
                Modifier_Id,
                Attribute_Id,
                Flat_Bonus,
                Percent_Bonus,
                Multiplicative_Bonus
            );
        }
    }
}
