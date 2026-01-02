using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IProjectService
    {
        Task<ServiceResult<Project>> AddProject(string name, string? description);
        Task<ServiceResult<Project>> GetProjectByPuid(string puid);
        Task<ServiceResult<List<Project>>> GetProjectsByUserPuid(string userPuid);
        Task<ServiceResult> DeleteProject(string puid);
    }
}