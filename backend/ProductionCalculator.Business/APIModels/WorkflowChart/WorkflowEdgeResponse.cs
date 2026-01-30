namespace ProductionCalculator.Business.APIModels
{
	public class WorkflowEdgeResponse
	{
		public string? Producer_Node_Puid { get; set; }
		public string? Consumer_Node_Puid { get; set; }
		public required string Product_Puid { get; set; }
		public required double Calculated_Flow_Rate { get; set; }
		public required double Actual_Flow_Rate { get; set; }
		public required bool Is_External { get; set; }
	}
}
