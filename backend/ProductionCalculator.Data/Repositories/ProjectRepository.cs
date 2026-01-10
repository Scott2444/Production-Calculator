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
    }
}
