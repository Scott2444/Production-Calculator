namespace ProductionCalculator.Business.Models
{
	public class WorkflowTarget
	{
		public required int Workflow_Target_Id { get; set; }
		public required int Workflow_Id { get; set; }
		public required int Product_Id { get; set; }
		public required double Target_Rate { get; set; }

		public bool ValueEquals(WorkflowTarget other)
		{
			if (other == null) return false;
			return Workflow_Target_Id == other.Workflow_Target_Id
				&& Workflow_Id == other.Workflow_Id
				&& Product_Id == other.Product_Id
				&& Target_Rate == other.Target_Rate;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Workflow_Target_Id, Workflow_Id, Product_Id, Target_Rate);
		}
	}
}
