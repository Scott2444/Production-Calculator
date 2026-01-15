using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
	public class MachineRecipeRepository : IMachineRecipeRepository
	{
		private readonly ProductionCalculatorDbContext _db;

		public MachineRecipeRepository(ProductionCalculatorDbContext db)
		{
			_db = db;
		}

		public async Task<MachineRecipe?> GetById(int id)
		{
			return await _db.Set<MachineRecipe>().FindAsync(id);
		}

		public async Task<IEnumerable<MachineRecipe>> GetByMachineId(int machineId)
		{
			return await _db.Set<MachineRecipe>()
				.Where(mr => mr.Machine_Id == machineId)
				.ToListAsync();
		}

        public async Task AddMachineRecipes(IEnumerable<MachineRecipe> machineRecipes)
        {
            await _db.Set<MachineRecipe>().AddRangeAsync(machineRecipes);
            await _db.SaveChangesAsync();
        }

		public async Task<bool> DeleteMachineRecipe(int id)
		{
			var machineRecipe = await _db.Set<MachineRecipe>().FindAsync(id);
			if (machineRecipe == null) return false;

			_db.Set<MachineRecipe>().Remove(machineRecipe);
			await _db.SaveChangesAsync();
			return true;
		}

		public async Task<List<bool>> DeleteMachineRecipes(IEnumerable<int> ids)
		{
			var results = new List<bool>();
			foreach (var id in ids)
			{
				var result = await DeleteMachineRecipe(id);
				results.Add(result);
			}
			return results;
		}
	}
}
