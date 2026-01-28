using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IWorkflowNodeService
    {
        Task<NodeChart> GetByWorkflowId(int workflowId, bool isTracked = false);
        Task CompleteWorkflowUpdate(int workflowId, NodeChart nodeChart);
    }
}
