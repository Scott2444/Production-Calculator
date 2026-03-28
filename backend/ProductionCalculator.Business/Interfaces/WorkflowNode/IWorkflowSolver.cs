using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Records;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IWorkflowSolver
    {
        Dictionary<int, double> SolveDemand(ProjectObjects projectObjects, NodeChart nodeChart);
        SolverSupplyResult SolveSupply(ProjectObjects projectObjects, NodeChart nodeChart);
    }
}