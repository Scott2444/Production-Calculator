using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Repositories
{
    public class RecipeAttributeRepository : IRecipeAttributeRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public RecipeAttributeRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task<RecipeAttribute?> GetById(int id)
        {
            return await _db.Set<RecipeAttribute>().FindAsync(id);
        }

        public async Task<IEnumerable<RecipeAttribute>> GetByRecipeId(int recipeId)
        {
            return await _db.Set<RecipeAttribute>()
                .Where(ra => ra.Recipe_Id == recipeId)
                .ToListAsync();
        }

        public async Task AddRecipeAttributes(IEnumerable<RecipeAttribute> recipeAttributes)
        {
            var recipeAttributeList = recipeAttributes.ToList();
            if (!recipeAttributeList.Any()) return;

            await _db.Set<RecipeAttribute>().AddRangeAsync(recipeAttributeList);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateRecipeAttributes(IEnumerable<RecipeAttribute> recipeAttributes)
        {
            var recipeAttributeList = recipeAttributes.ToList();
            if (!recipeAttributeList.Any()) return;

            _db.Set<RecipeAttribute>().UpdateRange(recipeAttributeList);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> DeleteRecipeAttribute(int id)
        {
            var recipeAttribute = await _db.Set<RecipeAttribute>().FindAsync(id);
            if (recipeAttribute == null) return false;

            _db.Set<RecipeAttribute>().Remove(recipeAttribute);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<bool>> DeleteRecipeAttributes(IEnumerable<int> ids)
        {
            var results = new List<bool>();
            foreach (var id in ids)
            {
                var result = await DeleteRecipeAttribute(id);
                results.Add(result);
            }

            return results;
        }
    }
}
