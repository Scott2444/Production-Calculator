namespace ProductionCalculator.Business.APIModels
{
	public class WorkflowChartResponse
	{
		public required List<WorkflowNodeResponse> Nodes { get; set; } = [];
        public required List<WorkflowEdgeResponse> Edges { get; set; } = [];
        public required List<WorkflowTargetExchange> Targets { get; set; } = [];
		public required List<WorkflowProductNodeResponse> ProductNodes { get; set; } = [];
	}
}
