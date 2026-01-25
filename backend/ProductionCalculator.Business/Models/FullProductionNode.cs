namespace ProductionCalculator.Business.Models
{
	public class FullProductionNode
	{
		public required int Node_Id { get; set; }
		public required int Workflow_Id { get; set; }
		public required string Puid { get; set; }
		public required int Product_Id { get; set; }
		public required int Product_Version { get; set; }
		public int? Recipe_Id { get; set; }
		public required int Recipe_Version { get; set; }
		public int? Machine_Id { get; set; }
		public required int Machine_Version { get; set; }
		public int? Parent_Node_Id { get; set; }
		public required double Target_Rate { get; set; }
		public required double Ideal_Machine_Count { get; set; }
		public required bool Is_Root { get; set; }
		public required bool Is_External { get; set; }
        public required List<ProductionNodeInput> Inputs { get; set; }
        public required List<ProductionNodeModifier> Modifiers { get; set; }
        public required ProductionNodeState State { get; set; }
		public required DateTime Created_At { get; set; }
		public required DateTime Last_Updated { get; set; }
	}
}
