using System;
namespace ProductionCalculator.Business.Models
{
	public class ProductionNodeState
	{
		public required int Node_Id { get; set; }
		public required double Actual_Machine_Count { get; set; }
		public double? External_Supply_Rate { get; set; }
		public required double Realized_Recipe_Rate { get; set; }

		public bool ValueEquals(ProductionNodeState other)
		{
			if (other == null) return false;
			return Node_Id == other.Node_Id
				&& Actual_Machine_Count == other.Actual_Machine_Count
				&& External_Supply_Rate == other.External_Supply_Rate
				&& Realized_Recipe_Rate == other.Realized_Recipe_Rate;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Node_Id, Actual_Machine_Count, External_Supply_Rate, Realized_Recipe_Rate);
		}
	}
}
