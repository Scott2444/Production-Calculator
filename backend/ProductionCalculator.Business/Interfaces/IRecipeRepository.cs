using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IRecipeRepository
    {
        Task<Recipe?> GetById(int id);
        Task<Recipe?> GetByPuid(string puid);
        Task<List<Recipe>> GetByProjectId(int projectId);
        Task AddRecipe(Recipe recipe);
        Task<Recipe> UpdateRecipe(Recipe recipe);
        Task<bool> DeleteRecipe(int id);
        Task<bool> PuidExists(string puid);
    }
}