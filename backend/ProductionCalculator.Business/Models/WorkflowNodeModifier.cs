namespace ProductionCalculator.Business.Models
{
	public class WorkflowNodeModifier
	{
		public required int Workflow_Node_Modifier_Id { get; set; }
		public required int Workflow_Node_Id { get; set; }
		public required int Modifier_Id { get; set; }
		public required int Modifier_Version { get; set; }

		public bool ValueEquals(WorkflowNodeModifier other)
		{
			if (other == null) return false;
			return Workflow_Node_Modifier_Id == other.Workflow_Node_Modifier_Id
				&& Workflow_Node_Id == other.Workflow_Node_Id
				&& Modifier_Id == other.Modifier_Id
				&& Modifier_Version == other.Modifier_Version;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Workflow_Node_Modifier_Id, Workflow_Node_Id, Modifier_Id, Modifier_Version);
		}
	}
}
