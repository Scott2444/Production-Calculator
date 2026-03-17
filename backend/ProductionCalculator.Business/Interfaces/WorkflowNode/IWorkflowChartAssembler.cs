using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Records;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IWorkflowChartAssembler
    {
        Task<NodeChart> RebuildChartNodes(NodeChart currentChart, Dictionary<int, double> recipeRates, ProjectObjects projectObjects, Workflow workflow, Func<string, Task<bool>> puidExistsFunc);
        NodeChart RebuildChartEdges(NodeChart currentChart, NodeChart updatedChart, ProjectObjects projectObjects);
        NodeChart UpdateChartRates(NodeChart chart, SolverSupplyResult solverSupplyResult, ProjectObjects projectObjects);
        NodeChart PruneDeletedComponents(NodeChart chart, ProjectObjects projectObjects);
    }
}