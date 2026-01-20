using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Helpers;

/**
 * Project last modified date should be updated when products are written to
 * !
 * !
 * ! 
 * !
*/


namespace ProductionCalculator.Business.Services
{
    public class ModifierService : IModifierService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IProjectRepository _projectRepo;
        private readonly IModifierRepository _repo;
    
        public ModifierService(
            ICurrentUserService currentUser,
            IProjectRepository projectRepo,
            IModifierRepository repo)
        {
            _currentUser = currentUser;
            _projectRepo = projectRepo;
            _repo = repo;
        }
        public async Task<ServiceResult<Modifier>> AddModifier(string projectPuid, string name, string? description, double flat_speed_bonus, double additive_percent_bonus, double multiplicative_modifiers)
        {
            if (string.IsNullOrWhiteSpace(name)) return ServiceResult<Modifier>.Fail(ServiceStatus.BadRequest400, "Modifier name is required.");

            // Get projectId from projectPuid
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<Modifier>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Check if name already exists for this project
            var existingModifiers = await _repo.GetModifiersByProjectId(project.Project_Id);
            if (existingModifiers.Any(p => p.Name == name)) return ServiceResult<Modifier>.Fail(ServiceStatus.Conflict409, "Modifier name already exists for this project.");

            // Limit string lengths
            name = TruncateHelper.TruncateString(name, 255);
            description = TruncateHelper.TruncateStringNullable(description, 1000);

            // Generate new PUID
            var puid = await PuidHelper.GenerateUniquePuidAsync(_repo.PuidExists);

            var modifier = new Modifier
            {
                Modifier_Id = 0,
                Project_Id = project.Project_Id,
                Puid = puid,
                Name = name,
                Description = description ?? string.Empty,
                Flat_Speed_Bonus = flat_speed_bonus,
                Additive_Percent_Bonus = additive_percent_bonus,
                Multiplicative_Modifiers = multiplicative_modifiers,
                Created_At = DateTime.UtcNow,
                Last_Updated = DateTime.UtcNow
            };

            await _repo.AddModifier(modifier);
            return ServiceResult<Modifier>.SuccessResult(modifier, ServiceStatus.Created201);
        }
        public async Task<ServiceResult<Modifier>> UpdateModifier(string projectPuid, string puid, string? name, string? description, double flat_speed_bonus, double additive_percent_bonus, double multiplicative_modifiers)
        {
            if (string.IsNullOrWhiteSpace(name)) return ServiceResult<Modifier>.Fail(ServiceStatus.BadRequest400, "Modifier name is required.");

            // Get projectId from projectPuid
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<Modifier>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Check if machine exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
            var modifier = await _repo.GetModifierByPuid(puid);
            if (modifier == null || modifier.Project_Id != project.Project_Id) return ServiceResult<Modifier>.Fail(ServiceStatus.NotFound404, "Modifier not found.");

            // Check if name already exists for this project
            var existingModifiers = await _repo.GetModifiersByProjectId(project.Project_Id);
            if (existingModifiers.Any(p => p.Name == name && p.Puid != puid)) return ServiceResult<Modifier>.Fail(ServiceStatus.Conflict409, "Modifier name already exists for this project.");

            // Limit string lengths
            name = TruncateHelper.TruncateString(name, 255);
            description = TruncateHelper.TruncateStringNullable(description, 1000);

            modifier.Name = name;
            modifier.Description = description ?? string.Empty;
            modifier.Flat_Speed_Bonus = flat_speed_bonus;
            modifier.Additive_Percent_Bonus = additive_percent_bonus;
            modifier.Multiplicative_Modifiers = multiplicative_modifiers;
            modifier.Last_Updated = DateTime.UtcNow;

            await _repo.UpdateModifier(modifier);
            return ServiceResult<Modifier>.SuccessResult(modifier, ServiceStatus.Ok200);
        }
        public async Task<ServiceResult<Modifier>> GetModifierByPuid(string projectPuid, string puid)
        {
            // Get projectId from projectPuid
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<Modifier>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Redirect aliased project to canonical project PUID
            if (!string.IsNullOrWhiteSpace(project.Alias_Project_Puid))
            {
                return ServiceResult<Modifier>.Redirection(ServiceStatus.SeeOther303, $"/api/projects/{project.Alias_Project_Puid}/modifiers/{puid}");
            }

            // Check if modifier exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
            var modifier = await _repo.GetModifierByPuid(puid);
            if (modifier == null || modifier.Project_Id != project.Project_Id) return ServiceResult<Modifier>.Fail(ServiceStatus.NotFound404, "Modifier not found.");

            return ServiceResult<Modifier>.SuccessResult(modifier);
        }
        public async Task<ServiceResult<List<Modifier>>> GetModifiersByProjectPuid(string projectPuid)
        {
            // Authorization already checked if project exists, otherwise they would not have access to it
            // i.e. this should never fail
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<List<Modifier>>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Redirect aliased project to canonical project PUID
            if (!string.IsNullOrWhiteSpace(project.Alias_Project_Puid))
            {
                return ServiceResult<List<Modifier>>.Redirection(ServiceStatus.SeeOther303, $"/api/projects/{project.Alias_Project_Puid}/modifiers");
            }

            var modifiers = await _repo.GetModifiersByProjectId(project.Project_Id);

            return ServiceResult<List<Modifier>>.SuccessResult(modifiers);
        }
        public async Task<ServiceResult> DeleteModifier(string projectPuid, string puid)
        {
            // Get projectId from projectPuid
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Check if modifier exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
            var modifier = await _repo.GetModifierByPuid(puid);
            if (modifier == null || modifier.Project_Id != project.Project_Id) return ServiceResult.Fail(ServiceStatus.NotFound404, "Modifier not found.");

            var isDeleted = await _repo.DeleteModifier(modifier.Modifier_Id);
            if (!isDeleted) return ServiceResult.Fail(ServiceStatus.InternalServerError500, "Failed to delete modifier.");

            return ServiceResult.SuccessResult(ServiceStatus.NoContent204);
        }
    }
}
