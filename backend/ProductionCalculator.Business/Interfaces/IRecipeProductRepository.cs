using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IRecipeProductRepository
    {
        Task<RecipeProduct?> GetById(int id);
        Task<IEnumerable<RecipeProduct>> GetByRecipeId(int recipeId);
        Task AddRecipeProducts(IEnumerable<RecipeProduct> recipeProducts);
        Task UpdateRecipeProducts(IEnumerable<RecipeProduct> recipeProducts);
        Task<bool> DeleteRecipeProduct(int id);
        Task<List<bool>> DeleteRecipeProducts(IEnumerable<int> ids);
    }
}
