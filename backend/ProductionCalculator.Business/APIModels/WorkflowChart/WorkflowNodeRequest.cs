namespace ProductionCalculator.Business.APIModels
{
	public class WorkflowNodeRequest
	{
		public required string MachinePuid { get; set; }
		public required double ActualMachineCount { get; set; }
		public required List<string> ModifierPuids { get; set; }
	}
}
