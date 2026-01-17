using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
    public class ModifierRepository : IModifierRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public ModifierRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task AddModifier(Modifier modifier)
        {
            await _db.Set<Modifier>().AddAsync(modifier);
            await _db.SaveChangesAsync();
        }

        public async Task<Modifier?> GetModifierById(int id)
        {
            return await _db.Set<Modifier>().FindAsync(id);
        }
        public async Task<Modifier?> GetModifierByPuid(string puid)
        {
            return await _db.Set<Modifier>().FirstOrDefaultAsync(p => p.Puid == puid);
        }
        public async Task<List<Modifier>> GetModifiersByProjectId(int projectId)
        {
            return await _db.Set<Modifier>().Where(p => p.Project_Id == projectId).ToListAsync();
        }
        public async Task<Modifier> UpdateModifier(Modifier modifier)
        {
            _db.Set<Modifier>().Update(modifier);
            await _db.SaveChangesAsync();
            return modifier;
        }
        public async Task<bool> DeleteModifier(int id) {
            var modifier = await _db.Set<Modifier>().FindAsync(id);
            if (modifier == null) return false;

            _db.Set<Modifier>().Remove(modifier);
            await _db.SaveChangesAsync();
            return true;
        }
        public async Task<bool> PuidExists(string puid)
        {
            return await _db.Set<Modifier>().AnyAsync(p => p.Puid == puid);
        }
    }
}
