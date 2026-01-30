namespace ProductionCalculator.Business.APIModels
{
	public class WorkflowChartResponse
	{
		public List<WorkflowNodeResponse> Nodes { get; set; } = [];
        public List<WorkflowEdgeResponse> Edges { get; set; } = [];
        public List<WorkflowTargetExchange> Targets { get; set; } = [];
	}
}
