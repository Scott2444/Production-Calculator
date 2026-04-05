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

        public async Task<bool> TryIncrementProductCount(string puid, int maxAllowed)
        {
            return await TryIncrementProjectCounterWithLimit(puid, maxAllowed, ProjectCounterType.Product);
        }

        public async Task DecrementProductCount(string puid)
        {
            await AdjustProjectCounter(puid, -1, ProjectCounterType.Product);
        }

        public async Task<bool> TryIncrementRecipeCount(string puid, int maxAllowed)
        {
            return await TryIncrementProjectCounterWithLimit(puid, maxAllowed, ProjectCounterType.Recipe);
        }

        public async Task DecrementRecipeCount(string puid)
        {
            await AdjustProjectCounter(puid, -1, ProjectCounterType.Recipe);
        }

        public async Task<bool> TryIncrementMachineCount(string puid, int maxAllowed)
        {
            return await TryIncrementProjectCounterWithLimit(puid, maxAllowed, ProjectCounterType.Machine);
        }

        public async Task DecrementMachineCount(string puid)
        {
            await AdjustProjectCounter(puid, -1, ProjectCounterType.Machine);
        }

        public async Task<bool> TryIncrementModifierCount(string puid, int maxAllowed)
        {
            return await TryIncrementProjectCounterWithLimit(puid, maxAllowed, ProjectCounterType.Modifier);
        }

        public async Task DecrementModifierCount(string puid)
        {
            await AdjustProjectCounter(puid, -1, ProjectCounterType.Modifier);
        }

        public async Task<bool> TryIncrementAttributeCount(string puid, int maxAllowed)
        {
            return await TryIncrementProjectCounterWithLimit(puid, maxAllowed, ProjectCounterType.Attribute);
        }

        public async Task DecrementAttributeCount(string puid)
        {
            await AdjustProjectCounter(puid, -1, ProjectCounterType.Attribute);
        }

        public async Task<bool> TryIncrementWorkflowCount(string puid, int maxAllowed)
        {
            return await TryIncrementProjectCounterWithLimit(puid, maxAllowed, ProjectCounterType.Workflow);
        }

        public async Task DecrementWorkflowCount(string puid)
        {
            await AdjustProjectCounter(puid, -1, ProjectCounterType.Workflow);
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

        public async Task<Project?> GetOldestAliasOfProject(string puid)
        {
            return await _db.Set<Project>()
                .Where(p => p.Alias_Project_Puid == puid)
                .OrderBy(p => p.Created_At)
                .FirstOrDefaultAsync();
        }

        private enum ProjectCounterType
        {
            Product,
            Recipe,
            Machine,
            Modifier,
            Attribute,
            Workflow
        }

        private async Task<bool> TryIncrementProjectCounterWithLimit(string puid, int maxAllowed, ProjectCounterType counter)
        {
            if (string.IsNullOrWhiteSpace(puid) || maxAllowed <= 0) return false;

            if (_db.Database.IsRelational())
            {
                var column = GetProjectCounterColumn(counter);
                var sql = $@"
                    update app.projects
                    set {column} = {column} + 1
                    where puid = {{0}}
                      and {column} < {{1}}";

                var affected = await _db.Database.ExecuteSqlRawAsync(sql, puid, maxAllowed);
                return affected > 0;
            }

            var project = await _db.Set<Project>().FirstOrDefaultAsync(p => p.Puid == puid);
            if (project == null) return false;

            var currentCount = GetProjectCounterValue(project, counter);
            if (currentCount >= maxAllowed) return false;

            SetProjectCounterValue(project, counter, currentCount + 1);
            await _db.SaveChangesAsync();
            return true;
        }

        private async Task AdjustProjectCounter(string puid, int delta, ProjectCounterType counter)
        {
            if (string.IsNullOrWhiteSpace(puid)) return;

            if (_db.Database.IsRelational())
            {
                var column = GetProjectCounterColumn(counter);
                var sql = delta >= 0
                    ? $@"
                        update app.projects
                        set {column} = {column} + {{1}}
                        where puid = {{0}}"
                    : $@"
                        update app.projects
                        set {column} = greatest({column} + {{1}}, 0)
                        where puid = {{0}}";

                await _db.Database.ExecuteSqlRawAsync(sql, puid, delta);
                return;
            }

            var project = await _db.Set<Project>().FirstOrDefaultAsync(p => p.Puid == puid);
            if (project == null) return;

            var currentCount = GetProjectCounterValue(project, counter);
            SetProjectCounterValue(project, counter, Math.Max(currentCount + delta, 0));
            await _db.SaveChangesAsync();
        }

        private static string GetProjectCounterColumn(ProjectCounterType counter)
        {
            return counter switch
            {
                ProjectCounterType.Product => "product_count",
                ProjectCounterType.Recipe => "recipe_count",
                ProjectCounterType.Machine => "machine_count",
                ProjectCounterType.Modifier => "modifier_count",
                ProjectCounterType.Attribute => "attribute_count",
                ProjectCounterType.Workflow => "workflow_count",
                _ => throw new ArgumentOutOfRangeException(nameof(counter), counter, null)
            };
        }

        private static int GetProjectCounterValue(Project project, ProjectCounterType counter)
        {
            return counter switch
            {
                ProjectCounterType.Product => project.Product_Count,
                ProjectCounterType.Recipe => project.Recipe_Count,
                ProjectCounterType.Machine => project.Machine_Count,
                ProjectCounterType.Modifier => project.Modifier_Count,
                ProjectCounterType.Attribute => project.Attribute_Count,
                ProjectCounterType.Workflow => project.Workflow_Count,
                _ => throw new ArgumentOutOfRangeException(nameof(counter), counter, null)
            };
        }

        /// <summary> 
        /// Required for unit testing when not using a relational database. 
        /// </summary>
        private static void SetProjectCounterValue(Project project, ProjectCounterType counter, int value)
        {
            switch (counter)
            {
                case ProjectCounterType.Product:
                    project.Product_Count = value;
                    break;
                case ProjectCounterType.Recipe:
                    project.Recipe_Count = value;
                    break;
                case ProjectCounterType.Machine:
                    project.Machine_Count = value;
                    break;
                case ProjectCounterType.Modifier:
                    project.Modifier_Count = value;
                    break;
                case ProjectCounterType.Attribute:
                    project.Attribute_Count = value;
                    break;
                case ProjectCounterType.Workflow:
                    project.Workflow_Count = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(counter), counter, null);
            }
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
