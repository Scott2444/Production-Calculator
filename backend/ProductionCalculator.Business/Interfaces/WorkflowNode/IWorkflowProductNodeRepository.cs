using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IWorkflowProductNodeRepository
    {
        Task<List<WorkflowProductNode>> GetByWorkflowId(int workflowId, bool isTracked);
        Task AddWorkflowProductNodes(List<WorkflowProductNode> workflowProductNodes);
        Task UpdateWorkflowProductNodes(List<WorkflowProductNode> workflowProductNodes);
        Task<bool> DeleteWorkflowProductNodes(List<int> ids);
    }
}