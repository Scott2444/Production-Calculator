using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IWorkflowNodeRepository
    {
        Task<List<WorkflowNode>> GetByWorkflow(int workflowId, bool isTracked = false);
        Task AddWorkflowNodes(List<WorkflowNode> workflowNodes);
        Task UpdateWorkflowNodes(List<WorkflowNode> workflowNodes);
        Task<bool> DeleteWorkflowNodes(List<int> ids);
    }
}