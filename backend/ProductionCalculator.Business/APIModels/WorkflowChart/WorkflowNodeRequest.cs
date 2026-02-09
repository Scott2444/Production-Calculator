namespace ProductionCalculator.Business.APIModels
{
	public class WorkflowNodeRequest
	{
		public string? MachinePuid { get; set; }
		public List<string> ModifierPuids { get; set; } = new List<string>();
		public double ActualMachineCount { get; set; }
	}
}
