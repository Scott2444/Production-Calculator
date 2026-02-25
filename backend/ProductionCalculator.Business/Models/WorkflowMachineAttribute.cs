namespace ProductionCalculator.Business.Models
{
    public class WorkflowMachineAttribute
    {
        public required int Workflow_Machine_Attribute_Id { get; set; }
        public required int Workflow_Node_Id { get; set; }
        public required int Attribute_Id { get; set; }
        public required double Rate { get; set; }

        public bool ValueEquals(WorkflowMachineAttribute other)
        {
            if (other == null) return false;
            return Workflow_Machine_Attribute_Id == other.Workflow_Machine_Attribute_Id
                && Workflow_Node_Id == other.Workflow_Node_Id
                && Attribute_Id == other.Attribute_Id
                && Rate == other.Rate;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Workflow_Machine_Attribute_Id, Workflow_Node_Id, Attribute_Id, Rate);
        }
    }
}
