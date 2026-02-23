using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IRecipeAttributeRepository
    {
        Task<RecipeAttribute?> GetById(int id);
        Task<IEnumerable<RecipeAttribute>> GetByRecipeId(int recipeId);
        Task AddRecipeAttributes(IEnumerable<RecipeAttribute> recipeAttributes);
        Task UpdateRecipeAttributes(IEnumerable<RecipeAttribute> recipeAttributes);
        Task<bool> DeleteRecipeAttribute(int id);
        Task<List<bool>> DeleteRecipeAttributes(IEnumerable<int> ids);
    }
}
