using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IWorkflowChartValidator
    {
        bool WorkflowIsUpToDate(NodeChart nodeChart, ProjectObjects projectObjects);
    }
}