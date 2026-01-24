
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
	public class WorkflowRepository : IWorkflowRepository
	{
		private readonly ProductionCalculatorDbContext _db;

		public WorkflowRepository(ProductionCalculatorDbContext db)
		{
			_db = db;
		}

		public async Task AddWorkflow(Workflow workflow)
		{
			await _db.Set<Workflow>().AddAsync(workflow);
			await _db.SaveChangesAsync();
		}

		public async Task<Workflow?> GetWorkflowById(int id)
		{
			return await _db.Set<Workflow>().FindAsync(id);
		}

		public async Task<Workflow?> GetWorkflowByPuid(string puid)
		{
			return await _db.Set<Workflow>().FirstOrDefaultAsync(w => w.Puid == puid);
		}

		public async Task<List<Workflow>> GetWorkflowsByProjectId(int projectId)
		{
			return await _db.Set<Workflow>().Where(w => w.Project_Id == projectId).ToListAsync();
		}

		public async Task<Workflow> UpdateWorkflow(Workflow workflow)
		{
			_db.Set<Workflow>().Update(workflow);
			await _db.SaveChangesAsync();
			return workflow;
		}

		public async Task<bool> DeleteWorkflow(int id)
		{
			var workflow = await _db.Set<Workflow>().FindAsync(id);
			if (workflow == null) return false;

			_db.Set<Workflow>().Remove(workflow);
			await _db.SaveChangesAsync();
			return true;
		}

		public async Task<bool> PuidExists(string puid)
		{
			return await _db.Set<Workflow>().AnyAsync(w => w.Puid == puid);
		}
	}
}
