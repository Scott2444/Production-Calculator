using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Repositories
{
    public class ModifierAttributeRepository : IModifierAttributeRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public ModifierAttributeRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task<ModifierAttribute?> GetById(int id)
        {
            return await _db.Set<ModifierAttribute>().FindAsync(id);
        }

        public async Task<IEnumerable<ModifierAttribute>> GetByModifierId(int modifierId)
        {
            return await _db.Set<ModifierAttribute>()
                .Where(ma => ma.Modifier_Id == modifierId)
                .ToListAsync();
        }

        public async Task AddModifierAttributes(IEnumerable<ModifierAttribute> modifierAttributes)
        {
            var modifierAttributeList = modifierAttributes.ToList();
            if (!modifierAttributeList.Any()) return;

            await _db.Set<ModifierAttribute>().AddRangeAsync(modifierAttributeList);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateModifierAttributes(IEnumerable<ModifierAttribute> modifierAttributes)
        {
            var modifierAttributeList = modifierAttributes.ToList();
            if (!modifierAttributeList.Any()) return;

            _db.Set<ModifierAttribute>().UpdateRange(modifierAttributeList);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> DeleteModifierAttribute(int id)
        {
            var modifierAttribute = await _db.Set<ModifierAttribute>().FindAsync(id);
            if (modifierAttribute == null) return false;

            _db.Set<ModifierAttribute>().Remove(modifierAttribute);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<bool>> DeleteModifierAttributes(IEnumerable<int> ids)
        {
            var results = new List<bool>();
            foreach (var id in ids)
            {
                var result = await DeleteModifierAttribute(id);
                results.Add(result);
            }

            return results;
        }
    }
}
