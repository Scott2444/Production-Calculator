using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
    public class WorkflowMachineAttributeRepository : IWorkflowMachineAttributeRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public WorkflowMachineAttributeRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task<List<WorkflowMachineAttribute>> GetByNodeId(int workflowNodeId, bool isTracked = false)
        {
            var query = _db.Set<WorkflowMachineAttribute>().Where(a => a.Workflow_Node_Id == workflowNodeId);
            return isTracked ? await query.ToListAsync() : await query.AsNoTracking().ToListAsync();
        }

        public async Task AddWorkflowMachineAttributes(List<WorkflowMachineAttribute> workflowMachineAttributes)
        {
            await _db.Set<WorkflowMachineAttribute>().AddRangeAsync(workflowMachineAttributes);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateWorkflowMachineAttributes(List<WorkflowMachineAttribute> workflowMachineAttributes)
        {
            _db.Set<WorkflowMachineAttribute>().UpdateRange(workflowMachineAttributes);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> DeleteWorkflowMachineAttributes(List<int> ids)
        {
            var attributes = await _db.Set<WorkflowMachineAttribute>()
                .Where(a => ids.Contains(a.Workflow_Machine_Attribute_Id))
                .ToListAsync();
            if (attributes.Count == 0) return false;
            _db.Set<WorkflowMachineAttribute>().RemoveRange(attributes);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
