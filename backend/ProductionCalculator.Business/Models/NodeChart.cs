namespace ProductionCalculator.Business.Models
{
	public class NodeChart
	{
		public required List<FullNode> Nodes { get; set; } = [];
        public required List<WorkflowEdge> Edges { get; set; } = [];
        public required List<WorkflowTarget> Targets { get; set; } = [];
	}
}
