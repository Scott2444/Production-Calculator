using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IWorkflowTargetRepository
    {
        Task<List<WorkflowTarget>> GetByWorkflowId(int workflowId, bool isTracked);
        Task AddWorkflowTargets(List<WorkflowTarget> workflowTargets);
        Task UpdateWorkflowTargets(List<WorkflowTarget> workflowTargets);
        Task<bool> DeleteWorkflowTargets(List<int> ids);
    }
}