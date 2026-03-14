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
		public List<WorkflowModifierExchange> Modifiers { get; set; } = [];
		// Use AttributeRateRequest here since attribute links don't have datetime info in workflow data
		public List<AttributeRateRequest> RecipeAttributes { get; set; } = [];
		public List<AttributeRateRequest> MachineAttributes { get; set; } = [];
	}
}
