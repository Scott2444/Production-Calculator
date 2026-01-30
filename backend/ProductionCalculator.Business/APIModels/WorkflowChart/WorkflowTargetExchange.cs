namespace ProductionCalculator.Business.APIModels
{
	public class WorkflowTargetExchange
	{
		public required string ProductPuid { get; set; }
        public required double TargetRate { get; set; }
	}
}
