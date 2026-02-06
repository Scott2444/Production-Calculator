namespace ProductionCalculator.Business.Models
{
	public class WorkflowEdge
	{
		public required int Workflow_Edge_Id { get; set; }
		public required int Workflow_Id { get; set; }
		public int? Producer_Node_Id { get; set; }
		public int? Consumer_Node_Id { get; set; }
		public required int Product_Node_Id { get; set; }
		public required double Calculated_Flow_Rate { get; set; }
		public required double Actual_Flow_Rate { get; set; }

		public bool ValueEquals(WorkflowEdge other)
		{
			if (other == null) return false;
			return Workflow_Edge_Id == other.Workflow_Edge_Id
				&& Workflow_Id == other.Workflow_Id
				&& Producer_Node_Id == other.Producer_Node_Id
				&& Consumer_Node_Id == other.Consumer_Node_Id
				&& Product_Node_Id == other.Product_Node_Id
				&& Calculated_Flow_Rate == other.Calculated_Flow_Rate
				&& Actual_Flow_Rate == other.Actual_Flow_Rate;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Workflow_Edge_Id, Workflow_Id, Producer_Node_Id, Consumer_Node_Id, Product_Node_Id, Calculated_Flow_Rate, Actual_Flow_Rate);
		}
	}
}
