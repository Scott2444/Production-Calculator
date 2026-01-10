using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IProjectRepository
    {
        Task AddProject(Project project);
        Task UpdateProject(Project project);
        Task<Project?> GetProjectById(int id);
        Task<Project?> GetProjectByPuid(string puid);
        Task<List<Project>> GetProjectsByUserId(int userId);
        Task<bool> DeleteProject(int id);
        Task<bool> PuidExists(string puid);
    }
}