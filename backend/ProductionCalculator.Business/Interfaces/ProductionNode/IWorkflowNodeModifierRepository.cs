using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IWorkflowNodeModifierRepository
    {
        Task<List<WorkflowNodeModifier>> GetByNodeId(int nodeId, bool isTracked = false);
        Task AddWorkflowNodeModifiers(List<WorkflowNodeModifier> workflowNodeModifiers);
        Task UpdateWorkflowNodeModifiers(List<WorkflowNodeModifier> workflowNodeModifiers);
        Task<bool> DeleteWorkflowNodeModifiers(List<int> ids);
    }
}