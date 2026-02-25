using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IWorkflowRecipeAttributeRepository
    {
        Task<List<WorkflowRecipeAttribute>> GetByNodeId(int workflowNodeId, bool isTracked = false);
        Task AddWorkflowRecipeAttributes(List<WorkflowRecipeAttribute> workflowRecipeAttributes);
        Task UpdateWorkflowRecipeAttributes(List<WorkflowRecipeAttribute> workflowRecipeAttributes);
        Task<bool> DeleteWorkflowRecipeAttributes(List<int> ids);
    }
}
