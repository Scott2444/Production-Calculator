using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
    public class RecipeRepository : IRecipeRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public RecipeRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task AddRecipe(Recipe recipe)
        {
            await _db.Set<Recipe>().AddAsync(recipe);
            await _db.SaveChangesAsync();
        }
        public async Task<Recipe?> GetByPuid(string puid)
        {
            return await _db.Set<Recipe>()
                .FirstOrDefaultAsync(r => r.Puid == puid);
        }
        public async Task<Recipe?> GetById(int id)
        {
            return await _db.Set<Recipe>().FindAsync(id);
        }
        public async Task<IEnumerable<Recipe>> GetByProjectId(int projectId)
        {
            return await _db.Set<Recipe>()
                .Where(r => r.Project_Id == projectId)
                .ToListAsync();
        }
        public async Task<Recipe> UpdateRecipe(Recipe recipe)
        {
            _db.Set<Recipe>().Update(recipe);
            await _db.SaveChangesAsync();
            return recipe;
        }
        public async Task<bool> DeleteRecipe(int id)
        {
            var recipe = await _db.Set<Recipe>().FindAsync(id);
            if (recipe == null) return false;

            _db.Set<Recipe>().Remove(recipe);
            await _db.SaveChangesAsync();
            return true;
        }
        public async Task<bool> PuidExists(string puid)
        {
            return await _db.Set<Recipe>().AnyAsync(r => r.Puid == puid);
        }
    }
}
