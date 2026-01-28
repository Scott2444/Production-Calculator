using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
    public class WorkflowEdgeRepository : IWorkflowEdgeRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public WorkflowEdgeRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task<List<WorkflowEdge>> GetByWorkflow(int workflowId, bool isTracked = false)
        {
            var query = _db.Set<WorkflowEdge>().Where(e => e.Workflow_Id == workflowId);
            return isTracked ? await query.ToListAsync() : await query.AsNoTracking().ToListAsync();
        }

        public async Task AddWorkflowEdges(List<WorkflowEdge> workflowEdges)
        {
            await _db.Set<WorkflowEdge>().AddRangeAsync(workflowEdges);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateWorkflowEdges(List<WorkflowEdge> workflowEdges)
        {
            _db.Set<WorkflowEdge>().UpdateRange(workflowEdges);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> DeleteWorkflowEdges(List<int> ids)
        {
            var edges = await _db.Set<WorkflowEdge>().Where(e => ids.Contains(e.Workflow_Edge_Id)).ToListAsync();
            if (edges.Count == 0) return false;
            _db.Set<WorkflowEdge>().RemoveRange(edges);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
