using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
    public class ProductionNodeRepository : IProductionNodeRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public ProductionNodeRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task<List<ProductionNode>> GetByWorkflowId(int workflowId, bool isTracked = false)
        {
            var query = _db.Set<ProductionNode>().Where(p => p.Workflow_Id == workflowId);
            return isTracked ? await query.ToListAsync() : await query.AsNoTracking().ToListAsync();
        }

        public async Task AddProductionNodes(List<ProductionNode> productionNodes)
        {
            await _db.Set<ProductionNode>().AddRangeAsync(productionNodes);
            await _db.SaveChangesAsync();
        }

        public async Task<List<ProductionNode>> UpdateProductionNodes(List<ProductionNode> productionNodes)
        {
            _db.Set<ProductionNode>().UpdateRange(productionNodes);
            await _db.SaveChangesAsync();
            return productionNodes;
        }

        public async Task<bool> DeleteProductionNodes(List<int> ids)
        {
            var nodes = await _db.Set<ProductionNode>().Where(p => ids.Contains(p.Node_Id)).ToListAsync();
            if (nodes.Count == 0) return false;
            _db.Set<ProductionNode>().RemoveRange(nodes);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PuidExists(string puid)
        {
            return await _db.Set<ProductionNode>().AnyAsync(p => p.Puid == puid);
        }
    }
}
