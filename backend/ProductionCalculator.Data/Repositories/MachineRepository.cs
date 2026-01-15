using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
	public class MachineRepository : IMachineRepository
	{
		private readonly ProductionCalculatorDbContext _db;

		public MachineRepository(ProductionCalculatorDbContext db)
		{
			_db = db;
		}

		public async Task AddMachine(Machine machine)
		{
			await _db.Set<Machine>().AddAsync(machine);
			await _db.SaveChangesAsync();
		}

		public async Task<Machine?> GetMachineById(int id)
		{
			return await _db.Set<Machine>().FindAsync(id);
		}

		public async Task<Machine?> GetMachineByPuid(string puid)
		{
			return await _db.Set<Machine>().FirstOrDefaultAsync(m => m.Puid == puid);
		}

		public async Task<List<Machine>> GetMachinesByProjectId(int projectId)
		{
			return await _db.Set<Machine>()
				.Where(m => m.Project_Id == projectId)
				.ToListAsync();
		}

		public async Task<Machine> UpdateMachine(Machine machine)
		{
			_db.Set<Machine>().Update(machine);
			await _db.SaveChangesAsync();
			return machine;
		}

		public async Task<bool> DeleteMachine(int id)
		{
			var machine = await _db.Set<Machine>().FindAsync(id);
			if (machine == null) return false;
			_db.Set<Machine>().Remove(machine);
			await _db.SaveChangesAsync();
			return true;
		}

		public async Task<bool> PuidExists(string puid)
		{
			return await _db.Set<Machine>().AnyAsync(m => m.Puid == puid);
		}
	}
}
