using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
    public class WorkflowNodeModifierRepository : IWorkflowNodeModifierRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public WorkflowNodeModifierRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task<List<WorkflowNodeModifier>> GetByNodeId(int nodeId, bool isTracked = false)
        {
            var query = _db.Set<WorkflowNodeModifier>().Where(m => m.Workflow_Node_Id == nodeId);
            return isTracked ? await query.ToListAsync() : await query.AsNoTracking().ToListAsync();
        }

        public async Task AddWorkflowNodeModifiers(List<WorkflowNodeModifier> workflowNodeModifiers)
        {
            await _db.Set<WorkflowNodeModifier>().AddRangeAsync(workflowNodeModifiers);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateWorkflowNodeModifiers(List<WorkflowNodeModifier> workflowNodeModifiers)
        {
            _db.Set<WorkflowNodeModifier>().UpdateRange(workflowNodeModifiers);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> DeleteWorkflowNodeModifiers(List<int> ids)
        {
            var modifiers = await _db.Set<WorkflowNodeModifier>().Where(m => ids.Contains(m.Workflow_Node_Modifier_Id)).ToListAsync();
            if (modifiers.Count == 0) return false;
            _db.Set<WorkflowNodeModifier>().RemoveRange(modifiers);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
