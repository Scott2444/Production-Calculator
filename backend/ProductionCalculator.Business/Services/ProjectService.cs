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

        public async Task<ServiceResult<Project>> AddProject(string name, string? description, bool? isPublic, string? aliasProjectPuid)
        {
            if (string.IsNullOrWhiteSpace(name)) return ServiceResult<Project>.Fail(ServiceStatus.BadRequest400, "Project name is required.");

            // Get userId from current user
            var userId = _currentUser.UserId;
            if (userId == null) return ServiceResult<Project>.Fail(ServiceStatus.BadRequest400, "Unable to determine current user.");

            // Check if name already exists for this user
            var existingProjects = await _repo.GetProjectsByUserId(userId.Value);
            if (existingProjects.Any(p => p.Name == name)) return ServiceResult<Project>.Fail(ServiceStatus.Conflict409, "Project name already exists for this user.");

            // Check alias project validity
            if (!await CheckProjectAlias(aliasProjectPuid, userId.Value))
            {
                return ServiceResult<Project>.Fail(ServiceStatus.BadRequest400, "Alias project PUID is invalid.");
            }

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
                Is_Public = isPublic ?? false,
                Alias_Project_Puid = aliasProjectPuid,
                Created_At = DateTime.UtcNow,
                Last_Updated = DateTime.UtcNow
            };

            await _repo.AddProject(project);
            return ServiceResult<Project>.SuccessResult(project, ServiceStatus.Created201);
        }
        public async Task<ServiceResult<Project>> UpdateProject(string projectPuid, string name, string? description, bool? isPublic, string? aliasProjectPuid)
        {
            if (string.IsNullOrWhiteSpace(name)) return ServiceResult<Project>.Fail(ServiceStatus.BadRequest400, "Project name is required.");

            // Get userId from current user
            var userId = _currentUser.UserId;
            if (userId == null) return ServiceResult<Project>.Fail(ServiceStatus.BadRequest400, "Unable to determine current user.");

            var project = await _repo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<Project>.Fail(ServiceStatus.NotFound404, $"Project with PUID {projectPuid} not found.");

            // Check if name already exists for this user
            var existingProjects = await _repo.GetProjectsByUserId(userId.Value);
            if (existingProjects.Any(p => p.Name == name && p.Project_Id != project.Project_Id)) return ServiceResult<Project>.Fail(ServiceStatus.Conflict409, "Project name already exists for this user.");

            // Check alias project validity
            if (!await CheckProjectAlias(aliasProjectPuid, userId.Value, projectPuid))
            {
                return ServiceResult<Project>.Fail(ServiceStatus.BadRequest400, "Alias project PUID is invalid.");
            }

            // Limit description length
            if (description != null && description.Length > 1000)
            {
                description = description.Substring(0, 1000);
            }

            project.Name = name;
            project.Description = description;
            project.Is_Public = isPublic ?? false;
            project.Alias_Project_Puid = aliasProjectPuid;
            project.Last_Updated = DateTime.UtcNow;

            await _repo.UpdateProject(project);
            return ServiceResult<Project>.SuccessResult(project, ServiceStatus.Ok200);
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
            Console.WriteLine($"Found {projects.Count} projects for user PUID {userPuid}");
            for (int i = 0; i < projects.Count; i++)
            {
                Console.WriteLine($"Project {i + 1}: PUID={projects[i].Puid}, Name={projects[i].Name}, alias={projects[i].Alias_Project_Puid}");
            }
            return ServiceResult<List<Project>>.SuccessResult(projects, ServiceStatus.Ok200);
        }
        public async Task<ServiceResult> DeleteProject(string puid)
        {
            if (string.IsNullOrWhiteSpace(puid)) return ServiceResult.Fail(ServiceStatus.BadRequest400);

            var project = await _repo.GetProjectByPuid(puid);
            if (project == null) return ServiceResult.Fail(ServiceStatus.NotFound404, $"Project with PUID {puid} not found.");

            var success = await _repo.DeleteProject(project.Project_Id);
            if (!success) return ServiceResult.Fail(ServiceStatus.InternalServerError500, "Failed to delete project.");

            return ServiceResult.SuccessResult(ServiceStatus.NoContent204);
        }

        /// <summary>
        /// Checks if the project can use the alias provided
        /// If not alias provided, returns true
        /// </summary>
        private async Task<bool> CheckProjectAlias(string? aliasProjectPuid, int userId, string? currentProjectPuid = null)
        {
            if (string.IsNullOrWhiteSpace(aliasProjectPuid)) return true;

            if (currentProjectPuid != null && aliasProjectPuid == currentProjectPuid) return false;
            var aliasProject = await _repo.GetProjectByPuid(aliasProjectPuid);
            if (aliasProject == null) return false;
            if (aliasProject.User_Id != userId && !aliasProject.Is_Public) return false; // Check authorization
            return true;
        }
    }
}