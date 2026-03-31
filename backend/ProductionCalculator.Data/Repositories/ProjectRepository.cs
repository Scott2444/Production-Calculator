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

        public async Task<(List<Project> Projects, int TotalCount)> SearchPublicProjects(string searchQuery, int page, int pageSize)
        {
            var normalizedQuery = (searchQuery ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedQuery))
            {
                return (new List<Project>(), 0);
            }

            if (string.Equals(_db.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal))
            {
                return await SearchPublicProjectsPostgres(normalizedQuery, page, pageSize);
            }

            throw new NotSupportedException("SearchPublicProjects is only supported when using PostgreSQL.");
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

        private async Task<(List<Project> Projects, int TotalCount)> SearchPublicProjectsPostgres(string searchQuery, int page, int pageSize)
        {
            var offset = (page - 1) * pageSize;

            var rankedQuery = _db.Set<Project>()
                .AsNoTracking()
                .Where(project => project.Is_Public)
                .Select(project => new
                {
                    Project = project,
                    SearchVector = project.Search_Vector
                })
                .Where(entry =>
                    entry.SearchVector != null &&
                    entry.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("english", searchQuery)))
                .Select(entry => new
                {
                    entry.Project,
                    RelevanceScore = entry.SearchVector!.RankCoverDensity(EF.Functions.WebSearchToTsQuery("english", searchQuery)),
                    PopularityScore = Math.Log(entry.Project.Alias_Count + 1.0)
                });

            var totalCount = await rankedQuery.CountAsync();

            if (totalCount == 0)
            {
                return (new List<Project>(), 0);
            }

            var projects = await rankedQuery
                .OrderByDescending(entry => entry.RelevanceScore + entry.PopularityScore)
                .ThenByDescending(entry => entry.RelevanceScore)
                .ThenBy(entry => entry.Project.Project_Id)
                .Skip(offset)
                .Take(pageSize)
                .Select(entry => entry.Project)
                .ToListAsync();

            return (projects, totalCount);
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
