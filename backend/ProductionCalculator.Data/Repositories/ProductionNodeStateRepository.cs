using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
    public class ProductionNodeStateRepository : IProductionNodeStateRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public ProductionNodeStateRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task<ProductionNodeState?> GetByNodeId(int nodeId, bool isTracked = false)
        {
            var query = _db.Set<ProductionNodeState>().Where(p => p.Node_Id == nodeId);
            return isTracked ? await query.FirstOrDefaultAsync() : await query.AsNoTracking().FirstOrDefaultAsync();
        }

        public async Task AddProductionNodeStates(List<ProductionNodeState> productionNodeStates)
        {
            await _db.Set<ProductionNodeState>().AddRangeAsync(productionNodeStates);
            await _db.SaveChangesAsync();
        }

        public async Task<List<ProductionNodeState>> UpdateProductionNodeStates(List<ProductionNodeState> productionNodeStates)
        {
            _db.Set<ProductionNodeState>().UpdateRange(productionNodeStates);
            await _db.SaveChangesAsync();
            return productionNodeStates;
        }

        public async Task<bool> DeleteProductionNodeStates(List<int> ids)
        {
            // Use the correct key property for ProductionNodeState. Assuming 'Node_Id' is the key.
            var states = await _db.Set<ProductionNodeState>().Where(p => ids.Contains(p.Node_Id)).ToListAsync();
            if (states.Count == 0) return false;
            _db.Set<ProductionNodeState>().RemoveRange(states);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
