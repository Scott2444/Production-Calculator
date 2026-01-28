using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
    public class WorkflowTargetRepository : IWorkflowTargetRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public WorkflowTargetRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task<List<WorkflowTarget>> GetByWorkflowId(int workflowId, bool isTracked = false)
        {
            var query = _db.Set<WorkflowTarget>().Where(t => t.Workflow_Id == workflowId);
            return isTracked ? await query.ToListAsync() : await query.AsNoTracking().ToListAsync();
        }

        public async Task AddWorkflowTargets(List<WorkflowTarget> workflowTargets)
        {
            await _db.Set<WorkflowTarget>().AddRangeAsync(workflowTargets);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateWorkflowTargets(List<WorkflowTarget> workflowTargets)
        {
            _db.Set<WorkflowTarget>().UpdateRange(workflowTargets);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> DeleteWorkflowTargets(List<int> ids)
        {
            var targets = await _db.Set<WorkflowTarget>().Where(t => ids.Contains(t.Workflow_Target_Id)).ToListAsync();
            if (targets.Count == 0) return false;
            _db.Set<WorkflowTarget>().RemoveRange(targets);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
