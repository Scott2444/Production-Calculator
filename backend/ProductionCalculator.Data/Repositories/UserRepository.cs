using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public UserRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task AddUser(User user)
        {
            await _db.Set<User>().AddAsync(user);
            await _db.SaveChangesAsync();
        }

        public async Task<User?> GetById(int id)
        {
            return await _db.Set<User>().FindAsync(id);
        }

        public async Task<User?> GetByUsername(string username)
        {
            return await _db.Set<User>().FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetByEmail(string email)
        {
            return await _db.Set<User>().FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> DeleteUser(int id) {
            var user = await _db.Set<User>().FindAsync(id);
            if (user == null) return false;

            _db.Set<User>().Remove(user);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<string> GetPasswordHash(int id)
        {
            var user = await _db.Set<User>().FindAsync(id);
            return user?.Password_Hash ?? string.Empty;
        }
    }
}
