namespace ProductionCalculator.Business.APIModels
{
	public class WorkflowNodeResponse
	{
		public required string Puid { get; set; }
		public required string Recipe_Puid { get; set; }
		public required bool Is_Preferred { get; set; }
		public string? Machine_Puid { get; set; }
		public double? Actual_Machine_Count { get; set; }
		public double? Calculated_Machine_Count { get; set; }
		public double? Calculated_Target_Rate { get; set; }
		public double? Calculated_Actual_Rate { get; set; }
        public List<string> ModifierPuids { get; set; } = [];
	}
}
