namespace ProductionCalculator.Business.Models
{
	public class FullWorkflowModifier
	{
		public required WorkflowNodeModifier Modifier { get; set; }
		public List<WorkflowModifierAttribute> ModifierAttributes { get; set; } = [];
	}
}
