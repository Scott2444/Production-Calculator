
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
	public class WorkflowRecipeRepository : IWorkflowRecipeRepository
	{
		private readonly ProductionCalculatorDbContext _db;

		public WorkflowRecipeRepository(ProductionCalculatorDbContext db)
		{
			_db = db;
		}

		public async Task<List<WorkflowRecipe>> GetByWorkflowId(int workflowId, bool isTracked)
		{
			var query = _db.Set<WorkflowRecipe>().Where(r => r.Workflow_Id == workflowId);
			return isTracked ? await query.ToListAsync() : await query.AsNoTracking().ToListAsync();
		}

		public async Task AddWorkflowRecipes(List<WorkflowRecipe> workflowRecipes)
		{
			await _db.Set<WorkflowRecipe>().AddRangeAsync(workflowRecipes);
			await _db.SaveChangesAsync();
		}

		public async Task UpdateWorkflowRecipes(List<WorkflowRecipe> workflowRecipes)
		{
			_db.Set<WorkflowRecipe>().UpdateRange(workflowRecipes);
			await _db.SaveChangesAsync();
		}

		public async Task<bool> DeleteWorkflowRecipes(List<int> ids)
		{
			var recipes = await _db.Set<WorkflowRecipe>().Where(r => ids.Contains(r.Workflow_Recipe_Id)).ToListAsync();
			if (recipes.Count == 0) return false;
			_db.Set<WorkflowRecipe>().RemoveRange(recipes);
			await _db.SaveChangesAsync();
			return true;
		}
	}
}
