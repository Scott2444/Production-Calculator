using System;
namespace ProductionCalculator.Business.Models
{
	public class WorkflowNode
	{
		public required int Node_Id { get; set; }
		public required int Workflow_Id { get; set; }
		public required string Puid { get; set; }
		public required int Recipe_Id { get; set; }
		public required int Recipe_Version { get; set; }
		public required bool Is_Preferred { get; set; }
		public int? Machine_Id { get; set; }
		public int? Machine_Version { get; set; }
		public double? Actual_Machine_Count { get; set; }
		public double? Calculated_Machine_Count { get; set; }
		public double? Calculated_Target_Rate { get; set; }
		public double? Calculated_Actual_Rate { get; set; }

		public bool ValueEquals(WorkflowNode other)
		{
			if (other == null) return false;
			return Node_Id == other.Node_Id
				&& Workflow_Id == other.Workflow_Id
				&& Puid == other.Puid
				&& Recipe_Id == other.Recipe_Id
				&& Recipe_Version == other.Recipe_Version
				&& Is_Preferred == other.Is_Preferred
				&& Machine_Id == other.Machine_Id
				&& Machine_Version == other.Machine_Version
				&& Actual_Machine_Count == other.Actual_Machine_Count
				&& Calculated_Machine_Count == other.Calculated_Machine_Count
				&& Calculated_Target_Rate == other.Calculated_Target_Rate
				&& Calculated_Actual_Rate == other.Calculated_Actual_Rate;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(
				(Node_Id, Workflow_Id, Puid, Recipe_Id, Recipe_Version, Is_Preferred, Machine_Id, Machine_Version, Actual_Machine_Count, Calculated_Machine_Count, Calculated_Target_Rate, Calculated_Actual_Rate)
			);
		}
	}
}
