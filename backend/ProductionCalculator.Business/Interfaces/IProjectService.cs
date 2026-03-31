using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.APIModels;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IProjectService
    {
        Task<ServiceResult<ProjectResponse>> AddProject(string name, string? description, bool? isPublic, string? aliasProjectPuid);
        Task<ServiceResult<ProjectResponse>> UpdateProject(string projectPuid, string name, string? description, bool? isPublic, string? aliasProjectPuid);
        Task<ServiceResult<ProjectResponse>> GetProjectByPuid(string puid);
        Task<ServiceResult<List<ProjectResponse>>> GetProjectsByUserPuid(string userPuid);
        Task<ServiceResult<PublicProjectSearchPageResponse>> SearchPublicProjects(string query, int page, int pageSize);
        Task<ServiceResult> DeleteProject(string puid);
        Task<ServiceResult<List<ProjectResponse>>> ResolveProject(string username, string? projectName);
    }
}