using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
    public class RecipeProductRepository : IRecipeProductRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public RecipeProductRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task<RecipeProduct?> GetById(int id)
        {
            return await _db.Set<RecipeProduct>().FindAsync(id);
        }
        public async Task<IEnumerable<RecipeProduct>> GetByRecipeId(int recipeId)
        {
            return await _db.Set<RecipeProduct>()
                .Where(rp => rp.Recipe_Id == recipeId)
                .ToListAsync();
        }
        public async Task UpsertRecipeProducts(IEnumerable<RecipeProduct> recipeProducts)
        {
            foreach (var recipeProduct in recipeProducts)
            {
                var existing = await GetById(recipeProduct.Recipe_Product_Id);
                if (existing == null)
                {
                    await _db.Set<RecipeProduct>().AddAsync(recipeProduct);
                }
                else
                {
                    _db.Set<RecipeProduct>().Update(recipeProduct);
                }
            }
            await _db.SaveChangesAsync();
        }
        public async Task<bool> DeleteRecipeProduct(int id)
        {
            var recipeProduct = await _db.Set<RecipeProduct>().FindAsync(id);
            if (recipeProduct == null) return false;

            _db.Set<RecipeProduct>().Remove(recipeProduct);
            await _db.SaveChangesAsync();
            return true;
        }
        public async Task<List<bool>> DeleteRecipeProducts(IEnumerable<int> ids)
        {
            var results = new List<bool>();
            foreach (var id in ids)
            {
                var result = await DeleteRecipeProduct(id);
                results.Add(result);
            }
            return results;
        }
    }
}
