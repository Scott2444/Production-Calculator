namespace ProductionCalculator.Business.APIModels
{
	public class WorkflowNodeResponse
	{
		public required string Puid { get; set; }
		public required string RecipePuid { get; set; }
		public string? MachinePuid { get; set; }
		public double? ActualMachineCount { get; set; }
		public double? CalculatedMachineCount { get; set; }
		public double? CalculatedTargetRate { get; set; }
		public double? CalculatedActualRate { get; set; }
        public List<string> ModifierPuids { get; set; } = [];
	}
}
