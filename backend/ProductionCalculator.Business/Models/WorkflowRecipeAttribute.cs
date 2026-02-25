namespace ProductionCalculator.Business.Models
{
    public class WorkflowRecipeAttribute
    {
        public required int Workflow_Recipe_Attribute_Id { get; set; }
        public required int Workflow_Node_Id { get; set; }
        public required int Attribute_Id { get; set; }
        public required double Rate { get; set; }

        public bool ValueEquals(WorkflowRecipeAttribute other)
        {
            if (other == null) return false;
            return Workflow_Recipe_Attribute_Id == other.Workflow_Recipe_Attribute_Id
                && Workflow_Node_Id == other.Workflow_Node_Id
                && Attribute_Id == other.Attribute_Id
                && Rate == other.Rate;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Workflow_Recipe_Attribute_Id, Workflow_Node_Id, Attribute_Id, Rate);
        }
    }
}
