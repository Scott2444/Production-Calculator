using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
    public class WorkflowModifierAttributeRepository : IWorkflowModifierAttributeRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public WorkflowModifierAttributeRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task<List<WorkflowModifierAttribute>> GetByNodeId(int workflowNodeId, bool isTracked = false)
        {
            var query = _db.Set<WorkflowModifierAttribute>().Where(a => a.Workflow_Node_Id == workflowNodeId);
            return isTracked ? await query.ToListAsync() : await query.AsNoTracking().ToListAsync();
        }

        public async Task AddWorkflowModifierAttributes(List<WorkflowModifierAttribute> workflowModifierAttributes)
        {
            await _db.Set<WorkflowModifierAttribute>().AddRangeAsync(workflowModifierAttributes);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateWorkflowModifierAttributes(List<WorkflowModifierAttribute> workflowModifierAttributes)
        {
            _db.Set<WorkflowModifierAttribute>().UpdateRange(workflowModifierAttributes);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> DeleteWorkflowModifierAttributes(List<int> ids)
        {
            var attributes = await _db.Set<WorkflowModifierAttribute>()
                .Where(a => ids.Contains(a.Workflow_Modifier_Attribute_Id))
                .ToListAsync();
            if (attributes.Count == 0) return false;
            _db.Set<WorkflowModifierAttribute>().RemoveRange(attributes);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
