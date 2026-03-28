using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public ProjectRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task AddProject(Project project)
        {
            await _db.Set<Project>().AddAsync(project);
            await _db.SaveChangesAsync();
        }
        public async Task UpdateProject(Project project)
        {
            _db.Set<Project>().Update(project);
            await _db.SaveChangesAsync();
        }

        public async Task IncrementAliasCount(string puid)
        {
            await AdjustAliasCount(puid, 1);
        }

        public async Task DecrementAliasCount(string puid)
        {
            await AdjustAliasCount(puid, -1);
        }

        public async Task<Project?> GetProjectById(int id)
        {
            return await _db.Set<Project>().FindAsync(id);
        }
        public async Task<Project?> GetProjectByPuid(string puid)
        {
            return await _db.Set<Project>().FirstOrDefaultAsync(p => p.Puid == puid);
        }
        public async Task<List<Project>> GetProjectsByUserId(int userId)
        {
            return await _db.Set<Project>().Where(p => p.User_Id == userId).ToListAsync();
        }
        public async Task<bool> DeleteProject(int id) {
            var project = await _db.Set<Project>().FindAsync(id);
            if (project == null) return false;

            _db.Set<Project>().Remove(project);
            await _db.SaveChangesAsync();
            return true;
        }
        public async Task<bool> PuidExists(string puid)
        {
            return await _db.Set<Project>().AnyAsync(p => p.Puid == puid);
        }

        private async Task AdjustAliasCount(string puid, int delta)
        {
            if (string.IsNullOrWhiteSpace(puid)) return;

            if (_db.Database.IsRelational())
            {
                if (delta >= 0)
                {
                    await _db.Database.ExecuteSqlInterpolatedAsync($@"
                        update app.projects
                        set alias_count = alias_count + {delta}
                        where puid = {puid}");
                }
                else
                {
                    await _db.Database.ExecuteSqlInterpolatedAsync($@"
                        update app.projects
                        set alias_count = greatest(alias_count + {delta}, 0)
                        where puid = {puid}");
                }

                return;
            }

            var project = await _db.Set<Project>().FirstOrDefaultAsync(p => p.Puid == puid);
            if (project == null) return;

            project.Alias_Count = Math.Max(project.Alias_Count + delta, 0);
            await _db.SaveChangesAsync();
        }
    }
}
