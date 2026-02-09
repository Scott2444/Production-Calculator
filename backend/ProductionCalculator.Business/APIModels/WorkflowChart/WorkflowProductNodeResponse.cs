namespace ProductionCalculator.Business.APIModels
{
	public class WorkflowProductNodeResponse
	{
		public required string ProductPuid { get; set; }
		public required double CalculatedFlowRate { get; set; }
		public required double ActualFlowRateIn { get; set; }
        public required double ActualFlowRateOut { get; set; }
        public required bool IsExternal { get; set; }
	}
}
