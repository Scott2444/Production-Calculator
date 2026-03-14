using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Helpers;
using ProductionCalculator.Business.APIModels;
using Microsoft.Extensions.Logging;

namespace ProductionCalculator.Business.Services
{
    public class ProjectService : IProjectService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IProjectRepository _repo;
        private readonly IUserRepository _userRepo;
        private readonly ILogger<ProjectService> _logger;
        public ProjectService(ICurrentUserService currentUser, IProjectRepository repo, IUserRepository userRepo, ILogger<ProjectService> logger) 
        { 
            _currentUser = currentUser; 
            _repo = repo;
            _userRepo = userRepo;
            _logger = logger;
        }

        private ProjectResponse MapToResponse(Project project, string ownerUsername)
        {
            return new ProjectResponse
            {
                Puid = project.Puid,
                Name = project.Name,
                OwnerUsername = ownerUsername,
                Description = project.Description,
                IsPublic = project.Is_Public,
                AliasProjectPuid = project.Alias_Project_Puid,
                CreatedAt = project.Created_At,
                UpdatedAt = project.Last_Updated
            };
        }

        // Use _currentUser.UserId or _currentUser.Username as needed

        public async Task<ServiceResult<ProjectResponse>> AddProject(string name, string? description, bool? isPublic, string? aliasProjectPuid)
        {
            if (string.IsNullOrWhiteSpace(name)) return ServiceResult<ProjectResponse>.Fail(ServiceStatus.BadRequest400, "Project name is required.");

            // Get userId from current user
            var userPuid = _currentUser.UserPuid;
            if (userPuid == null) return ServiceResult<ProjectResponse>.Fail(ServiceStatus.BadRequest400, "Unable to determine current user.");

            var user = await _userRepo.GetByPuid(userPuid);
            if (user == null) return ServiceResult<ProjectResponse>.Fail(ServiceStatus.BadRequest400, "Unable to determine current user.");

            // Check if name already exists for this user
            var existingProjects = await _repo.GetProjectsByUserId(user.User_Id);
            if (existingProjects.Any(p => p.Name == name)) return ServiceResult<ProjectResponse>.Fail(ServiceStatus.Conflict409, "Project name already exists for this user.");

            // Check alias project validity
            if (!await CheckProjectAlias(aliasProjectPuid, user.User_Id))
            {
                return ServiceResult<ProjectResponse>.Fail(ServiceStatus.BadRequest400, "Alias project PUID is invalid.");
            }

            // Limit string lengths
            name = TruncateHelper.TruncateString(name, 255);
            description = TruncateHelper.TruncateStringNullable(description, 1000);

            // Generate new PUID
            var puid = await PuidHelper.GenerateUniquePuidAsync(_repo.PuidExists);

            var project = new Project
            {
                Project_Id = 0,
                User_Id = user.User_Id,
                Puid = puid,
                Name = name,
                Description = description ?? string.Empty,
                Is_Public = isPublic ?? false,
                Alias_Project_Puid = aliasProjectPuid,
                Created_At = DateTime.UtcNow,
                Last_Updated = DateTime.UtcNow
            };

            await _repo.AddProject(project);
            _logger.LogInformation("Project state change: change: Project '{ProjectName}' (PUID: {ProjectPuid}) created by user {UserPuid}.", project.Name, project.Puid, user.Puid);
            return ServiceResult<ProjectResponse>.SuccessResult(MapToResponse(project, user.Username), ServiceStatus.Created201);
        }
        public async Task<ServiceResult<ProjectResponse>> UpdateProject(string projectPuid, string name, string? description, bool? isPublic, string? aliasProjectPuid)
        {
            if (string.IsNullOrWhiteSpace(name)) return ServiceResult<ProjectResponse>.Fail(ServiceStatus.BadRequest400, "Project name is required.");

            // Get userId from current user
            var userPuid = _currentUser.UserPuid;
            if (userPuid == null) return ServiceResult<ProjectResponse>.Fail(ServiceStatus.BadRequest400, "Unable to determine current user.");

            var user = await _userRepo.GetByPuid(userPuid);
            if (user == null) return ServiceResult<ProjectResponse>.Fail(ServiceStatus.BadRequest400, "Unable to determine current user.");

            var project = await _repo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<ProjectResponse>.Fail(ServiceStatus.NotFound404, $"Project with PUID {projectPuid} not found.");

            // Check if name already exists for this user
            var existingProjects = await _repo.GetProjectsByUserId(user.User_Id);
            if (existingProjects.Any(p => p.Name == name && p.Project_Id != project.Project_Id)) return ServiceResult<ProjectResponse>.Fail(ServiceStatus.Conflict409, "Project name already exists for this user.");

            // Check alias project validity
            if (!await CheckProjectAlias(aliasProjectPuid, user.User_Id, projectPuid))
            {
                return ServiceResult<ProjectResponse>.Fail(ServiceStatus.BadRequest400, "Alias project PUID is invalid.");
            }

            // Limit string lengths
            name = TruncateHelper.TruncateString(name, 255);
            description = TruncateHelper.TruncateStringNullable(description, 1000);

            project.Name = name;
            project.Description = description;
            project.Is_Public = isPublic ?? false;
            project.Alias_Project_Puid = aliasProjectPuid;
            project.Last_Updated = DateTime.UtcNow;

            await _repo.UpdateProject(project);
            _logger.LogInformation("Project state change: change: Project '{ProjectName}' (PUID: {ProjectPuid}) updated by user {UserPuid}.", project.Name, project.Puid, user.Puid);
            return ServiceResult<ProjectResponse>.SuccessResult(MapToResponse(project, user.Username), ServiceStatus.Ok200);
        }
        public async Task<ServiceResult<ProjectResponse>> GetProjectByPuid(string puid)
        {
            if (string.IsNullOrWhiteSpace(puid)) return ServiceResult<ProjectResponse>.Fail(ServiceStatus.BadRequest400);

            var project = await _repo.GetProjectByPuid(puid);
            if (project == null) return ServiceResult<ProjectResponse>.Fail(ServiceStatus.NotFound404, $"Project with PUID {puid} not found.");

            var user = await _userRepo.GetById(project.User_Id);
            if (user == null) return ServiceResult<ProjectResponse>.Fail(ServiceStatus.NotFound404, "Owner not found.");

            return ServiceResult<ProjectResponse>.SuccessResult(MapToResponse(project, user.Username), ServiceStatus.Ok200);
        }
        public async Task<ServiceResult<List<ProjectResponse>>> GetProjectsByUserPuid(string userPuid)
        {
            if (string.IsNullOrWhiteSpace(userPuid)) return ServiceResult<List<ProjectResponse>>.Fail(ServiceStatus.BadRequest400);

            var user = await _userRepo.GetByPuid(userPuid);
            if (user == null) return ServiceResult<List<ProjectResponse>>.Fail(ServiceStatus.NotFound404, $"User with PUID {userPuid} not found.");
            
            var projects = await _repo.GetProjectsByUserId(user.User_Id);
            var projectResponses = projects.Select(p => MapToResponse(p, user.Username)).ToList();
            return ServiceResult<List<ProjectResponse>>.SuccessResult(projectResponses, ServiceStatus.Ok200);
        }
        public async Task<ServiceResult> DeleteProject(string puid)
        {
            if (string.IsNullOrWhiteSpace(puid)) return ServiceResult.Fail(ServiceStatus.BadRequest400);

            var project = await _repo.GetProjectByPuid(puid);
            if (project == null) return ServiceResult.Fail(ServiceStatus.NotFound404, $"Project with PUID {puid} not found.");

            var success = await _repo.DeleteProject(project.Project_Id);
            if (!success)
            {
                _logger.LogError("DeleteProject failure: Failed to delete project {ProjectPuid} from repository.", puid);
                return ServiceResult.Fail(ServiceStatus.InternalServerError500, "Failed to delete project.");
            }

            _logger.LogInformation("Project state change: Project '{ProjectName}' (PUID: {ProjectPuid}) deleted.", project.Name, project.Puid);
            return ServiceResult.SuccessResult(ServiceStatus.NoContent204);
        }
        public async Task<ServiceResult<List<ProjectResponse>>> ResolveProject(string username, string? projectName)
        {
            if (string.IsNullOrWhiteSpace(username))
                return ServiceResult<List<ProjectResponse>>.Fail(ServiceStatus.BadRequest400);

            var user = await _userRepo.GetByUsername(username);
            if (user == null)
                return ServiceResult<List<ProjectResponse>>.Fail(ServiceStatus.NotFound404, $"Project or user not found.");

            var userProjects = await _repo.GetProjectsByUserId(user.User_Id);

            // Custom auth logic
            // Filter by public if not owner or admin
            var isOwnerOrAdmin = !string.IsNullOrWhiteSpace(_currentUser.UserPuid) &&
                _currentUser.UserPuid.Equals(user.Puid, StringComparison.Ordinal) || _currentUser.IsAdmin;
            if (!isOwnerOrAdmin)
            {
                userProjects = userProjects.Where(p => p.Is_Public).ToList();
            }

            List<Project> projects;

            if (string.IsNullOrWhiteSpace(projectName))
            {
                projects = userProjects;
            }
            else
            {
                var project = userProjects.FirstOrDefault(p => p.Name == projectName);
                projects = project != null ? new List<Project> { project } : new List<Project>();
            }

            if (!projects.Any())
                return ServiceResult<List<ProjectResponse>>.Fail(ServiceStatus.NotFound404, "Project or user not found.");

            var projectResponses = projects.Select(p => MapToResponse(p, user.Username)).ToList();
            return ServiceResult<List<ProjectResponse>>.SuccessResult(projectResponses, ServiceStatus.Ok200);
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