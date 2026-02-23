using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Repositories
{
    public class AttributeRepository : IAttributeRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public AttributeRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task AddAttribute(ProjectAttribute attribute)
        {
            await _db.Set<ProjectAttribute>().AddAsync(attribute);
            await _db.SaveChangesAsync();
        }

        public async Task<ProjectAttribute?> GetAttributeById(int id)
        {
            return await _db.Set<ProjectAttribute>().FindAsync(id);
        }

        public async Task<ProjectAttribute?> GetAttributeByPuid(string puid)
        {
            return await _db.Set<ProjectAttribute>().FirstOrDefaultAsync(a => a.Puid == puid);
        }

        public async Task<List<ProjectAttribute>> GetAttributesByProjectId(int projectId)
        {
            return await _db.Set<ProjectAttribute>().Where(a => a.Project_Id == projectId).ToListAsync();
        }

        public async Task<ProjectAttribute> UpdateAttribute(ProjectAttribute attribute)
        {
            _db.Set<ProjectAttribute>().Update(attribute);
            await _db.SaveChangesAsync();
            return attribute;
        }

        public async Task<bool> DeleteAttribute(int id)
        {
            var attribute = await _db.Set<ProjectAttribute>().FindAsync(id);
            if (attribute == null) return false;

            _db.Set<ProjectAttribute>().Remove(attribute);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PuidExists(string puid)
        {
            return await _db.Set<ProjectAttribute>().AnyAsync(a => a.Puid == puid);
        }
    }
}
