using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Helpers;

namespace ProductionCalculator.Business.Services
{
    public class ProjectService : IProjectService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IProjectRepository _repo;
        private readonly IUserRepository _userRepo;
        public ProjectService(ICurrentUserService currentUser, IProjectRepository repo, IUserRepository userRepo) 
        { 
            _currentUser = currentUser; 
            _repo = repo;
            _userRepo = userRepo;
        }

        // Use _currentUser.UserId or _currentUser.Username as needed

        public async Task<ServiceResult<Project>> AddProject(string name, string? description)
        {
            if (string.IsNullOrWhiteSpace(name)) return ServiceResult<Project>.Fail(ServiceStatus.BadRequest400, "Project name is required.");

            // Get userId from current user
            var userId = _currentUser.UserId;
            if (userId == null) return ServiceResult<Project>.Fail(ServiceStatus.BadRequest400, "Unable to determine current user.");

            // Check if name already exists for this user
            var existingProjects = await _repo.GetProjectsByUserId(userId.Value);
            if (existingProjects.Any(p => p.Name == name)) return ServiceResult<Project>.Fail(ServiceStatus.Conflict409, "Project name already exists for this user.");

            // Limit description length
            if (description != null && description.Length > 1000)
            {
                description = description.Substring(0, 1000);
            }

            // Generate new PUID
            var puid = await PuidHelper.GenerateUniquePuidAsync(_repo.PuidExists);

            var project = new Project
            {
                Project_Id = 0,
                User_Id = userId.Value,
                Puid = puid,
                Name = name,
                Description = description ?? string.Empty,
                Created_At = DateTime.UtcNow,
                Last_Updated = DateTime.UtcNow
            };

            await _repo.AddProject(project);
            return ServiceResult<Project>.SuccessResult(project, ServiceStatus.Created201);
        }
        public async Task<ServiceResult<Project>> GetProjectByPuid(string puid)
        {
            if (string.IsNullOrWhiteSpace(puid)) return ServiceResult<Project>.Fail(ServiceStatus.BadRequest400);

            var project = await _repo.GetProjectByPuid(puid);
            if (project == null) return ServiceResult<Project>.Fail(ServiceStatus.NotFound404, $"Project with PUID {puid} not found.");

            return ServiceResult<Project>.SuccessResult(project, ServiceStatus.Ok200);
        }
        public async Task<ServiceResult<List<Project>>> GetProjectsByUserPuid(string userPuid)
        {
            if (string.IsNullOrWhiteSpace(userPuid)) return ServiceResult<List<Project>>.Fail(ServiceStatus.BadRequest400);

            var user = await _userRepo.GetByPuid(userPuid);
            if (user == null) return ServiceResult<List<Project>>.Fail(ServiceStatus.NotFound404, $"User with PUID {userPuid} not found.");
            
            var projects = await _repo.GetProjectsByUserId(user.User_Id);
            return ServiceResult<List<Project>>.SuccessResult(projects, ServiceStatus.Ok200);
        }
        public async Task<ServiceResult> DeleteProject(string puid)
        {
            if (string.IsNullOrWhiteSpace(puid)) return ServiceResult.Fail(ServiceStatus.BadRequest400);

            var project = await _repo.GetProjectByPuid(puid);
            if (project == null) return ServiceResult.Fail(ServiceStatus.NotFound404, $"Project with PUID {puid} not found.");

            var success = await _repo.DeleteProject(project.Project_Id);
            if (!success) return ServiceResult.Fail(ServiceStatus.InternalServerError500, "Failed to delete project.");

            return ServiceResult.SuccessResult(ServiceStatus.Ok200);
        }
    }
}