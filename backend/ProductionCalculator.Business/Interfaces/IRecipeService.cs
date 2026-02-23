using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IRecipeService
    {
        Task<ServiceResult<RecipeResponse>> AddRecipe(string projectPuid, string name, string? description, double baseCraftingTime, List<RecipeProductExchange> inputs, List<RecipeProductExchange> outputs, List<AttributeRateExchange>? attributes = null);
        Task<ServiceResult<RecipeResponse>> GetRecipeByPuid(string projectPuid, string puid);
        Task<ServiceResult<List<RecipeResponse>>> GetRecipesByProjectPuid(string projectPuid);
        Task<ServiceResult<RecipeResponse>> UpdateRecipe(string projectPuid, string puid, string name, string? description, double baseCraftingTime, List<RecipeProductExchange> inputs, List<RecipeProductExchange> outputs, List<AttributeRateExchange>? attributes = null);
        Task<ServiceResult> DeleteRecipe(string projectPuid, string puid);
    }
}