using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IWorkflowModifierAttributeRepository
    {
        Task<List<WorkflowModifierAttribute>> GetByNodeId(int workflowNodeId, bool isTracked = false);
        Task AddWorkflowModifierAttributes(List<WorkflowModifierAttribute> workflowModifierAttributes);
        Task UpdateWorkflowModifierAttributes(List<WorkflowModifierAttribute> workflowModifierAttributes);
        Task<bool> DeleteWorkflowModifierAttributes(List<int> ids);
    }
}
