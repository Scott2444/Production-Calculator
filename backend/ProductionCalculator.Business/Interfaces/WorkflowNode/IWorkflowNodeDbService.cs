using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IWorkflowNodeDbService
    {
        Task<NodeChart> GetByWorkflowId(int workflowId, bool isTracked = false);
        Task<NodeChart> WorkflowUpdate(int workflowId, NodeChart nodeChart);
        Task<NodeChart> WorkflowEdgeUpdate(int workflowId, NodeChart nodeChart);
    }
}
