using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
	public class WorkflowProductNodeRepository : IWorkflowProductNodeRepository
	{
		private readonly ProductionCalculatorDbContext _db;

		public WorkflowProductNodeRepository(ProductionCalculatorDbContext db)
		{
			_db = db;
		}

		public async Task<List<WorkflowProductNode>> GetByWorkflowId(int workflowId, bool isTracked)
		{
			var query = _db.Set<WorkflowProductNode>().Where(n => n.Workflow_Id == workflowId);
			return isTracked ? await query.ToListAsync() : await query.AsNoTracking().ToListAsync();
		}

		public async Task AddWorkflowProductNodes(List<WorkflowProductNode> workflowProductNodes)
		{
			await _db.Set<WorkflowProductNode>().AddRangeAsync(workflowProductNodes);
			await _db.SaveChangesAsync();
		}

		public async Task UpdateWorkflowProductNodes(List<WorkflowProductNode> workflowProductNodes)
		{
			_db.Set<WorkflowProductNode>().UpdateRange(workflowProductNodes);
			await _db.SaveChangesAsync();
		}

		public async Task<bool> DeleteWorkflowProductNodes(List<int> ids)
		{
			var nodes = await _db.Set<WorkflowProductNode>().Where(n => ids.Contains(n.Workflow_Product_Node_Id)).ToListAsync();
			if (nodes.Count == 0) return false;
			_db.Set<WorkflowProductNode>().RemoveRange(nodes);
			await _db.SaveChangesAsync();
			return true;
		}
	}
}
