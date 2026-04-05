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
        public async Task UpdateUser(User user)
        {
            _db.Set<User>().Update(user);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> TryIncrementProjectCount(string puid, int maxAllowed)
        {
            if (string.IsNullOrWhiteSpace(puid) || maxAllowed <= 0) return false;

            if (_db.Database.IsRelational())
            {
                var affected = await _db.Database.ExecuteSqlRawAsync(@"
                    update app.users
                    set project_count = project_count + 1
                    where puid = {0}
                      and project_count < {1}", puid, maxAllowed);

                return affected > 0;
            }

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Puid == puid);
            if (user == null) return false;
            if (user.Project_Count >= maxAllowed) return false;

            user.Project_Count += 1;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task IncrementProjectCount(string puid)
        {
            await AdjustProjectCount(puid, 1);
        }

        public async Task DecrementProjectCount(string puid)
        {
            await AdjustProjectCount(puid, -1);
        }

        public async Task<User?> GetById(int id)
        {
            return await _db.Set<User>().FindAsync(id);
        }

        public async Task<User?> GetByUsername(string username)
        {
            return await _db.Set<User>().FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<User?> GetByEmail(string email)
        {
            return await _db.Set<User>().FirstOrDefaultAsync(u => u.Email == email);
        }
        public async Task<User?> GetByPuid(string puid)
        {
            return await _db.Set<User>().FirstOrDefaultAsync(u => u.Puid == puid);
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

        public async Task<bool> PuidExists(string puid)
        {
            return await _db.Set<User>().AnyAsync(u => u.Puid == puid);
        }

        private async Task AdjustProjectCount(string puid, int delta)
        {
            if (string.IsNullOrWhiteSpace(puid)) return;

            if (_db.Database.IsRelational())
            {
                if (delta >= 0)
                {
                    await _db.Database.ExecuteSqlRawAsync(@"
                        update app.users
                        set project_count = project_count + {1}
                        where puid = {0}", puid, delta);
                }
                else
                {
                    await _db.Database.ExecuteSqlRawAsync(@"
                        update app.users
                        set project_count = greatest(project_count + {1}, 0)
                        where puid = {0}", puid, delta);
                }

                return;
            }

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Puid == puid);
            if (user == null) return;

            user.Project_Count = Math.Max(user.Project_Count + delta, 0);
            await _db.SaveChangesAsync();
        }
    }
}
