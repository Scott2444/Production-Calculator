using System;
namespace ProductionCalculator.Business.Models
{
	public class ProductionNodeInput
	{
		public required int Node_Input_Id { get; set; }
		public required int Node_Id { get; set; }
		public required int Input_Product_Id { get; set; }
		public required double Required_Rate { get; set; }
		public required bool Is_Cyclic { get; set; }

		public bool ValueEquals(ProductionNodeInput other)
		{
			if (other == null) return false;
			return Node_Input_Id == other.Node_Input_Id
				&& Node_Id == other.Node_Id
				&& Input_Product_Id == other.Input_Product_Id
				&& Required_Rate == other.Required_Rate
				&& Is_Cyclic == other.Is_Cyclic;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Node_Input_Id, Node_Id, Input_Product_Id, Required_Rate, Is_Cyclic);
		}
	}
}
