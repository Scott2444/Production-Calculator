using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IProjectService
    {
        Task<ServiceResult<Project>> AddProject(string name, string? description, bool? isPublic, string? aliasProjectPuid);
        Task<ServiceResult<Project>> UpdateProject(string projectPuid, string name, string? description, bool? isPublic, string? aliasProjectPuid);
        Task<ServiceResult<Project>> GetProjectByPuid(string puid);
        Task<ServiceResult<List<Project>>> GetProjectsByUserPuid(string userPuid);
        Task<ServiceResult> DeleteProject(string puid);
        Task<ServiceResult<List<Project>>> ResolveProject(string username, string? projectName);
    }
}