using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public RoleRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task<Role?> GetRole(int id)
        {
            return await _db.Set<Role>().FirstOrDefaultAsync(r => r.Role_Id == id);
        }
        public async Task<Role?> GetRole(string roleName)
        {
            return await _db.Set<Role>().FirstOrDefaultAsync(r => r.Role_Name == roleName);
        }
    }
}
