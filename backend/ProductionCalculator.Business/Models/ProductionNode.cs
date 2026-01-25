using System;
namespace ProductionCalculator.Business.Models
{
	public class ProductionNode
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
		public required DateTime Created_At { get; set; }
		public required DateTime Last_Updated { get; set; }

		public bool ValueEquals(ProductionNode other)
		{
			if (other == null) return false;
			return Node_Id == other.Node_Id
				&& Workflow_Id == other.Workflow_Id
				&& Puid == other.Puid
				&& Product_Id == other.Product_Id
				&& Product_Version == other.Product_Version
				&& Recipe_Id == other.Recipe_Id
				&& Recipe_Version == other.Recipe_Version
				&& Machine_Id == other.Machine_Id
				&& Machine_Version == other.Machine_Version
				&& Parent_Node_Id == other.Parent_Node_Id
				&& Target_Rate == other.Target_Rate
				&& Ideal_Machine_Count == other.Ideal_Machine_Count
				&& Is_Root == other.Is_Root
				&& Is_External == other.Is_External
				&& Created_At == other.Created_At
				&& Last_Updated == other.Last_Updated;
		}

		public override int GetHashCode()
		{
			// HashCode.Combine supports up to 8 arguments, so we nest calls for all fields
			int hash1 = HashCode.Combine(Node_Id, Workflow_Id, Puid, Product_Id, Product_Version, Recipe_Id, Recipe_Version, Machine_Id);
			int hash2 = HashCode.Combine(Machine_Version, Parent_Node_Id, Target_Rate, Ideal_Machine_Count, Is_Root, Is_External, Created_At, Last_Updated);
			return HashCode.Combine(hash1, hash2);
		}
	}
}
