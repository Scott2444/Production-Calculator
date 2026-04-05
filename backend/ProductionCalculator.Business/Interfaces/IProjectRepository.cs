using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IProjectRepository
    {
        Task AddProject(Project project);
        Task UpdateProject(Project project);
        Task IncrementAliasCount(string puid);
        Task DecrementAliasCount(string puid);
        Task<bool> TryIncrementProductCount(string puid, int maxAllowed);
        Task DecrementProductCount(string puid);
        Task<bool> TryIncrementRecipeCount(string puid, int maxAllowed);
        Task DecrementRecipeCount(string puid);
        Task<bool> TryIncrementMachineCount(string puid, int maxAllowed);
        Task DecrementMachineCount(string puid);
        Task<bool> TryIncrementModifierCount(string puid, int maxAllowed);
        Task DecrementModifierCount(string puid);
        Task<bool> TryIncrementAttributeCount(string puid, int maxAllowed);
        Task DecrementAttributeCount(string puid);
        Task<bool> TryIncrementWorkflowCount(string puid, int maxAllowed);
        Task DecrementWorkflowCount(string puid);
        Task<Project?> GetProjectById(int id);
        Task<Project?> GetProjectByPuid(string puid);
        Task<List<Project>> GetProjectsByUserId(int userId);
        Task<(List<Project> Projects, int TotalCount)> SearchPublicProjects(string searchQuery, int page, int pageSize);
        Task<bool> DeleteProject(int id);
        Task<Project?> GetOldestAliasOfProject(string puid);
        Task<bool> PuidExists(string puid);
    }
}