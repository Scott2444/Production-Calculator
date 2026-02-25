namespace ProductionCalculator.Business.Models
{
	public class FullNode
	{
		public required WorkflowNode Node { get; set; }
		public List<FullWorkflowModifier> Modifiers { get; set; } = [];
		public List<WorkflowRecipeAttribute> RecipeAttributes { get; set; } = [];
		public List<WorkflowMachineAttribute> MachineAttributes { get; set; } = [];
	}
}
