using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
    public class WorkflowRecipeAttributeRepository : IWorkflowRecipeAttributeRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public WorkflowRecipeAttributeRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task<List<WorkflowRecipeAttribute>> GetByNodeId(int workflowNodeId, bool isTracked = false)
        {
            var query = _db.Set<WorkflowRecipeAttribute>().Where(a => a.Workflow_Node_Id == workflowNodeId);
            return isTracked ? await query.ToListAsync() : await query.AsNoTracking().ToListAsync();
        }

        public async Task AddWorkflowRecipeAttributes(List<WorkflowRecipeAttribute> workflowRecipeAttributes)
        {
            await _db.Set<WorkflowRecipeAttribute>().AddRangeAsync(workflowRecipeAttributes);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateWorkflowRecipeAttributes(List<WorkflowRecipeAttribute> workflowRecipeAttributes)
        {
            _db.Set<WorkflowRecipeAttribute>().UpdateRange(workflowRecipeAttributes);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> DeleteWorkflowRecipeAttributes(List<int> ids)
        {
            var attributes = await _db.Set<WorkflowRecipeAttribute>()
                .Where(a => ids.Contains(a.Workflow_Recipe_Attribute_Id))
                .ToListAsync();
            if (attributes.Count == 0) return false;
            _db.Set<WorkflowRecipeAttribute>().RemoveRange(attributes);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
