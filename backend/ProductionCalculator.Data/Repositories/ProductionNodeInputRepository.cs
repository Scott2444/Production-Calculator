using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
    public class ProductionNodeInputRepository : IProductionNodeInputRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public ProductionNodeInputRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task<List<ProductionNodeInput>> GetByNodeId(int nodeId, bool isTracked = false)
        {
            var query = _db.Set<ProductionNodeInput>().Where(p => p.Node_Id == nodeId);
            return isTracked ? await query.ToListAsync() : await query.AsNoTracking().ToListAsync();
        }

        public async Task AddProductionNodeInputs(List<ProductionNodeInput> productionNodeInputs)
        {
            await _db.Set<ProductionNodeInput>().AddRangeAsync(productionNodeInputs);
            await _db.SaveChangesAsync();
        }

        public async Task<List<ProductionNodeInput>> UpdateProductionNodeInputs(List<ProductionNodeInput> productionNodeInputs)
        {
            _db.Set<ProductionNodeInput>().UpdateRange(productionNodeInputs);
            await _db.SaveChangesAsync();
            return productionNodeInputs;
        }

        public async Task<bool> DeleteProductionNodeInputs(List<int> ids)
        {
            var inputs = await _db.Set<ProductionNodeInput>().Where(p => ids.Contains(p.Node_Id)).ToListAsync();
            if (inputs.Count == 0) return false;
            _db.Set<ProductionNodeInput>().RemoveRange(inputs);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
