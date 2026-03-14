namespace ProductionCalculator.Business.APIModels
{
	public class WorkflowNodeRequest
	{
		public required string MachinePuid { get; set; }
		public required double ActualMachineCount { get; set; }
		public required List<WorkflowModifierExchange> Modifiers { get; set; }
		public required List<AttributeRateRequest> RecipeAttributes { get; set; }
		public required List<AttributeRateRequest> MachineAttributes { get; set; }
	}
}
