using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
    public class ProductionNodeModifierRepository : IProductionNodeModifierRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public ProductionNodeModifierRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task<List<ProductionNodeModifier>> GetByNodeId(int nodeId, bool isTracked = false)
        {
            var query = _db.Set<ProductionNodeModifier>().Where(p => p.Node_Id == nodeId);
            return isTracked ? await query.ToListAsync() : await query.AsNoTracking().ToListAsync();
        }

        public async Task AddProductionNodeModifiers(List<ProductionNodeModifier> productionNodeModifiers)
        {
            await _db.Set<ProductionNodeModifier>().AddRangeAsync(productionNodeModifiers);
            await _db.SaveChangesAsync();
        }

        public async Task<List<ProductionNodeModifier>> UpdateProductionNodeModifiers(List<ProductionNodeModifier> productionNodeModifiers)
        {
            _db.Set<ProductionNodeModifier>().UpdateRange(productionNodeModifiers);
            await _db.SaveChangesAsync();
            return productionNodeModifiers;
        }

        public async Task<bool> DeleteProductionNodeModifiers(List<int> ids)
        {
            var modifiers = await _db.Set<ProductionNodeModifier>().Where(p => ids.Contains(p.Node_Id)).ToListAsync();
            if (modifiers.Count == 0) return false;
            _db.Set<ProductionNodeModifier>().RemoveRange(modifiers);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
