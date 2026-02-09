namespace ProductionCalculator.Business.Models
{
	public class WorkflowRecipe
	{
		public required int Workflow_Recipe_Id { get; set; }
		public required int Workflow_Id { get; set; }
		public required int Recipe_Id { get; set; }

        public bool ValueEquals(WorkflowRecipe other)
		{
			if (other == null) return false;
			return Workflow_Recipe_Id == other.Workflow_Recipe_Id
				&& Workflow_Id == other.Workflow_Id
				&& Recipe_Id == other.Recipe_Id;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Workflow_Recipe_Id, Workflow_Id, Recipe_Id);
		}
	}
}
