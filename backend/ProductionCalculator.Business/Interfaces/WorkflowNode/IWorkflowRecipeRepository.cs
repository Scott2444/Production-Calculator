using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IWorkflowRecipeRepository
    {
        Task<List<WorkflowRecipe>> GetByWorkflowId(int workflowId, bool isTracked);
        Task AddWorkflowRecipes(List<WorkflowRecipe> workflowRecipes);
        Task UpdateWorkflowRecipes(List<WorkflowRecipe> workflowRecipes);
        Task<bool> DeleteWorkflowRecipes(List<int> ids);
    }
}