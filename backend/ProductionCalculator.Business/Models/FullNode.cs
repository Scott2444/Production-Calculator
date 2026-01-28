namespace ProductionCalculator.Business.Models
{
	public class FullNode
	{
		public required WorkflowNode Node { get; set; }
		public List<WorkflowNodeModifier> Modifiers { get; set; } = [];
	}
}
