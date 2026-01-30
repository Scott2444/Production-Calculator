using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
    public class WorkflowNodeRepository : IWorkflowNodeRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public WorkflowNodeRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task<List<WorkflowNode>> GetByWorkflow(int workflowId, bool isTracked = false)
        {
            var query = _db.Set<WorkflowNode>().Where(n => n.Workflow_Id == workflowId);
            return isTracked ? await query.ToListAsync() : await query.AsNoTracking().ToListAsync();
        }

        public async Task AddWorkflowNodes(List<WorkflowNode> workflowNodes)
        {
            await _db.Set<WorkflowNode>().AddRangeAsync(workflowNodes);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateWorkflowNodes(List<WorkflowNode> workflowNodes)
        {
            _db.Set<WorkflowNode>().UpdateRange(workflowNodes);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> DeleteWorkflowNodes(List<int> ids)
        {
            var nodes = await _db.Set<WorkflowNode>().Where(n => ids.Contains(n.Node_Id)).ToListAsync();
            if (nodes.Count == 0) return false;
            _db.Set<WorkflowNode>().RemoveRange(nodes);
            await _db.SaveChangesAsync();
            return true;
        }
        public async Task<bool> PuidExists(string puid)
        {
            return await _db.Set<WorkflowNode>().AnyAsync(n => n.Puid == puid);
        }
    }
}
