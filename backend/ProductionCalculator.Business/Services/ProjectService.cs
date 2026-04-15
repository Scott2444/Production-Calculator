using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Helpers;
using ProductionCalculator.Business.APIModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace ProductionCalculator.Business.Services
{
    public class ProjectService : IProjectService
    {
        private const int MaxSearchPageSize = 100;

        private readonly ICurrentUserService _currentUser;
        private readonly IProjectRepository _repo;
        private readonly IUserRepository _userRepo;
        private readonly ILogger<ProjectService> _logger;
        private readonly int _maxProjectsPerUser;

        public ProjectService(ICurrentUserService currentUser, IProjectRepository repo, IUserRepository userRepo, ILogger<ProjectService> logger, IConfiguration? configuration = null) 
        { 
            _currentUser = currentUser; 
            _repo = repo;
            _userRepo = userRepo;
            _logger = logger;

            var objectLimits = ObjectLimitSettings.FromConfiguration(configuration);
            _maxProjectsPerUser = objectLimits.MaxProjectsPerUser;
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
                AliasCount = project.Alias_Count,
                ProductCount = project.Product_Count,
                RecipeCount = project.Recipe_Count,
                MachineCount = project.Machine_Count,
                ModifierCount = project.Modifier_Count,
                AttributeCount = project.Attribute_Count,
                WorkflowCount = project.Workflow_Count,
                CreatedAt = project.Created_At,
                UpdatedAt = project.Last_Updated
            };
        }

        // Use _currentUser.UserId or _currentUser.Username as needed

        public async Task<ServiceResult<ProjectResponse>> AddProject(string name, string? description, bool? isPublic, string? aliasProjectPuid)
        {
            if (string.IsNullOrWhiteSpace(name)) return ServiceResult<ProjectResponse>.Fail(ServiceStatus.BadRequest400, "Project name is required.");

            if (!string.IsNullOrEmpty(aliasProjectPuid) && isPublic.HasValue && isPublic.Value)
            {
                return ServiceResult<ProjectResponse>.Fail(ServiceStatus.BadRequest400, "Aliased projects cannot be public.");
            }

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

            if (!await _userRepo.TryIncrementProjectCount(user.Puid, _maxProjectsPerUser))
            {
                return ServiceResult<ProjectResponse>.Fail(ServiceStatus.Conflict409, $"Project limit reached. Maximum allowed per user is {_maxProjectsPerUser}.");
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
                Alias_Count = 0,
                Created_At = DateTime.UtcNow,
                Last_Updated = DateTime.UtcNow
            };

            try
            {
                await _repo.AddProject(project);
            }
            catch
            {
                await _userRepo.DecrementProjectCount(user.Puid);
                throw;
            }

            await ApplyAliasCountDelta(null, project.Alias_Project_Puid);
            _logger.LogInformation("Project state change: change: Project '{ProjectName}' (PUID: {ProjectPuid}) created by user {UserPuid}.", project.Name, project.Puid, user.Puid);
            return ServiceResult<ProjectResponse>.SuccessResult(MapToResponse(project, user.Username), ServiceStatus.Created201);
        }
        public async Task<ServiceResult<ProjectResponse>> UpdateProject(string projectPuid, string name, string? description, bool? isPublic, string? aliasProjectPuid)
        {
            if (string.IsNullOrWhiteSpace(name)) return ServiceResult<ProjectResponse>.Fail(ServiceStatus.BadRequest400, "Project name is required.");

            if (!string.IsNullOrEmpty(aliasProjectPuid) && isPublic.HasValue && isPublic.Value)
            {
                return ServiceResult<ProjectResponse>.Fail(ServiceStatus.BadRequest400, "Aliased projects cannot be public.");
            }

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

            var previousAliasProjectPuid = project.Alias_Project_Puid;
            project.Name = name;
            project.Description = description;
            project.Is_Public = isPublic ?? false;
            project.Alias_Project_Puid = aliasProjectPuid;
            project.Last_Updated = DateTime.UtcNow;

            await _repo.UpdateProject(project);
            await ApplyAliasCountDelta(previousAliasProjectPuid, project.Alias_Project_Puid);
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

        public async Task<ServiceResult<PublicProjectSearchPageResponse>> SearchPublicProjects(string query, int page, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return ServiceResult<PublicProjectSearchPageResponse>.Fail(ServiceStatus.BadRequest400, "Search query is required.");
            }

            if (page < 1)
            {
                return ServiceResult<PublicProjectSearchPageResponse>.Fail(ServiceStatus.BadRequest400, "Page must be at least 1.");
            }

            if (pageSize < 1 || pageSize > MaxSearchPageSize)
            {
                return ServiceResult<PublicProjectSearchPageResponse>.Fail(ServiceStatus.BadRequest400, $"Page size must be between 1 and {MaxSearchPageSize}.");
            }

            var normalizedQuery = query.Trim();
            var (projects, totalCount) = await _repo.SearchPublicProjects(normalizedQuery, page, pageSize);

            var ownerLookup = new Dictionary<int, string>();
            foreach (var project in projects)
            {
                if (ownerLookup.ContainsKey(project.User_Id))
                {
                    continue;
                }

                var owner = await _userRepo.GetById(project.User_Id);
                ownerLookup[project.User_Id] = owner?.Username ?? string.Empty;
            }

            var responses = projects
                .Select(project => MapToResponse(project, ownerLookup.GetValueOrDefault(project.User_Id, string.Empty)))
                .ToList();

            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
            var response = new PublicProjectSearchPageResponse
            {
                Projects = responses,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };

            return ServiceResult<PublicProjectSearchPageResponse>.SuccessResult(response, ServiceStatus.Ok200);
        }

        public async Task<ServiceResult> DeleteProject(string puid)
        {
            if (string.IsNullOrWhiteSpace(puid)) return ServiceResult.Fail(ServiceStatus.BadRequest400);

            var project = await _repo.GetProjectByPuid(puid);
            if (project == null) return ServiceResult.Fail(ServiceStatus.NotFound404, $"Project with PUID {puid} not found.");

            if (project.Alias_Count > 0)
            {
                // Transfer ownership to user of oldest alias
                // Preserve both projects
                var oldestAlias = await _repo.GetOldestAliasOfProject(puid);
                if (oldestAlias == null)
                {
                    _logger.LogError("DeleteProject failure: Expected an alias for project {ProjectPuid}, but none was found.", puid);
                    return ServiceResult.Fail(ServiceStatus.InternalServerError500, "Failed to transfer ownership during project deletion.");
                }

                var previousOwnerId = project.User_Id;
                project.User_Id = oldestAlias.User_Id;
                _logger.LogInformation("Project deletion: Project '{ProjectName}' (PUID: {ProjectPuid}) ownership transferred to user {UserId} due to deletion of original owner.", project.Name, project.Puid, oldestAlias.User_Id);
                await _repo.UpdateProject(project);

                if (previousOwnerId != oldestAlias.User_Id)
                {
                    var previousOwner = await _userRepo.GetById(previousOwnerId);
                    if (previousOwner != null)
                    {
                        await _userRepo.DecrementProjectCount(previousOwner.Puid);
                    }

                    var newOwner = await _userRepo.GetById(oldestAlias.User_Id);
                    if (newOwner != null)
                    {
                        await _userRepo.IncrementProjectCount(newOwner.Puid);
                    }
                }

                return ServiceResult.SuccessResult(ServiceStatus.NoContent204);
            }

            var previousAliasProjectPuid = project.Alias_Project_Puid;
            var projectOwner = await _userRepo.GetById(project.User_Id);

            var success = await _repo.DeleteProject(project.Project_Id);
            if (!success)
            {
                _logger.LogError("DeleteProject failure: Failed to delete project {ProjectPuid} from repository.", puid);
                return ServiceResult.Fail(ServiceStatus.InternalServerError500, "Failed to delete project.");
            }

            await ApplyAliasCountDelta(previousAliasProjectPuid, null);

            if (projectOwner != null)
            {
                await _userRepo.DecrementProjectCount(projectOwner.Puid);
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
                return ServiceResult<List<ProjectResponse>>.Fail(ServiceStatus.NotFound404, $"User not found.");

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
                if (project == null)
                {
                    return ServiceResult<List<ProjectResponse>>.Fail(ServiceStatus.NotFound404, $"Project with name '{projectName}' not found for user '{username}'.");
                }
                projects = new List<Project> { project };
            }

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

        private async Task ApplyAliasCountDelta(string? previousAliasProjectPuid, string? newAliasProjectPuid)
        {
            if (string.Equals(previousAliasProjectPuid, newAliasProjectPuid, StringComparison.Ordinal)) return;

            if (!string.IsNullOrWhiteSpace(previousAliasProjectPuid))
            {
                await _repo.DecrementAliasCount(previousAliasProjectPuid);
            }

            if (!string.IsNullOrWhiteSpace(newAliasProjectPuid))
            {
                await _repo.IncrementAliasCount(newAliasProjectPuid);
            }
        }
    }
}