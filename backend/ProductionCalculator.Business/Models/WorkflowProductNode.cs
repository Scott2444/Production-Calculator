namespace ProductionCalculator.Business.Models
{
	public class WorkflowProductNode
	{
		public required int Workflow_Product_Node_Id { get; set; }
		public required int Workflow_Id { get; set; }
		public required int Product_Id { get; set; }
		public required double Calculated_Flow_Rate { get; set; }
		public required double Actual_Flow_Rate { get; set; }
        public required bool Is_External { get; set; }

		public bool ValueEquals(WorkflowProductNode other)
		{
			if (other == null) return false;
			return Workflow_Product_Node_Id == other.Workflow_Product_Node_Id
				&& Workflow_Id == other.Workflow_Id
				&& Product_Id == other.Product_Id
				&& Calculated_Flow_Rate == other.Calculated_Flow_Rate
				&& Actual_Flow_Rate == other.Actual_Flow_Rate
                && Is_External == other.Is_External;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Workflow_Product_Node_Id, Workflow_Id, Product_Id, Calculated_Flow_Rate, Actual_Flow_Rate, Is_External);
		}
	}
}
