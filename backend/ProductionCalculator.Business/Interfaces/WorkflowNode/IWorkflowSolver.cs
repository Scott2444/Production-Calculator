using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IWorkflowSolver
    {
        Dictionary<int, double> SolveDemand(ProjectObjects projectObjects, NodeChart nodeChart);
        Dictionary<int, double> SolveSupply(ProjectObjects projectObjects, NodeChart nodeChart);
    }
}