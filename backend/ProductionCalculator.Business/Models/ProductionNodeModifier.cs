using System;
namespace ProductionCalculator.Business.Models
{
	public class ProductionNodeModifier
	{
		public required int Node_Modifier_Id { get; set; }
		public required int Node_Id { get; set; }
		public required int Modifier_Id { get; set; }
		public required int Modifier_Version { get; set; }

		public bool ValueEquals(ProductionNodeModifier other)
		{
			if (other == null) return false;
			return Node_Modifier_Id == other.Node_Modifier_Id
				&& Node_Id == other.Node_Id
				&& Modifier_Id == other.Modifier_Id
				&& Modifier_Version == other.Modifier_Version;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Node_Modifier_Id, Node_Id, Modifier_Id, Modifier_Version);
		}
	}
}
