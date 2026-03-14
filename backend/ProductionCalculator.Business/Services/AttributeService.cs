using ProductionCalculator.Business.Helpers;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Services
{
    public class AttributeService : IAttributeService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IAttributeRepository _repo;
        private readonly IProjectRepository _projectRepo;

        public AttributeService(ICurrentUserService currentUser, IAttributeRepository repo, IProjectRepository projectRepo)
        {
            _currentUser = currentUser;
            _repo = repo;
            _projectRepo = projectRepo;
        }

        public async Task<ServiceResult<ProjectAttribute>> AddAttribute(string projectPuid, string name, string? description, string? unit)
        {
            if (string.IsNullOrWhiteSpace(name)) return ServiceResult<ProjectAttribute>.Fail(ServiceStatus.BadRequest400, "Attribute name is required.");

            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<ProjectAttribute>.Fail(ServiceStatus.NotFound404, "Project not found.");

            var existingAttributes = await _repo.GetAttributesByProjectId(project.Project_Id);
            if (existingAttributes.Any(p => p.Name == name)) return ServiceResult<ProjectAttribute>.Fail(ServiceStatus.Conflict409, "Attribute name already exists for this project.");

            name = TruncateHelper.TruncateString(name, 255);
            description = TruncateHelper.TruncateStringNullable(description, 1000);
            unit = TruncateHelper.TruncateStringNullable(unit, 50);

            var puid = await PuidHelper.GenerateUniquePuidAsync(_repo.PuidExists);

            var attribute = new ProjectAttribute
            {
                Attribute_Id = 0,
                Project_Id = project.Project_Id,
                Puid = puid,
                Name = name,
                Description = description,
                Unit = unit,
                Version = 1,
                Created_At = DateTime.UtcNow,
                Last_Updated = DateTime.UtcNow
            };

            await _repo.AddAttribute(attribute);
            await UpdateProjectLastUpdated(project);
            return ServiceResult<ProjectAttribute>.SuccessResult(attribute, ServiceStatus.Created201);
        }

        public async Task<ServiceResult<ProjectAttribute>> GetAttributeByPuid(string projectPuid, string puid)
        {
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<ProjectAttribute>.Fail(ServiceStatus.NotFound404, "Project not found.");

            if (!string.IsNullOrWhiteSpace(project.Alias_Project_Puid))
            {
                return ServiceResult<ProjectAttribute>.Redirection(ServiceStatus.SeeOther303, $"/projects/{project.Alias_Project_Puid}/attributes/{puid}");
            }

            var attribute = await _repo.GetAttributeByPuid(puid);
            if (attribute == null || attribute.Project_Id != project.Project_Id) return ServiceResult<ProjectAttribute>.Fail(ServiceStatus.NotFound404, "Attribute not found.");

            return ServiceResult<ProjectAttribute>.SuccessResult(attribute);
        }

        public async Task<ServiceResult<List<ProjectAttribute>>> GetAttributesByProjectPuid(string projectPuid)
        {
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<List<ProjectAttribute>>.Fail(ServiceStatus.NotFound404, "Project not found.");

            if (!string.IsNullOrWhiteSpace(project.Alias_Project_Puid))
            {
                return ServiceResult<List<ProjectAttribute>>.Redirection(ServiceStatus.SeeOther303, $"/projects/{project.Alias_Project_Puid}/attributes");
            }

            var attributes = await _repo.GetAttributesByProjectId(project.Project_Id);
            return ServiceResult<List<ProjectAttribute>>.SuccessResult(attributes);
        }

        public async Task<ServiceResult<ProjectAttribute>> UpdateAttribute(string projectPuid, string puid, string? name, string? description, string? unit)
        {
            if (string.IsNullOrWhiteSpace(name)) return ServiceResult<ProjectAttribute>.Fail(ServiceStatus.BadRequest400, "Attribute name is required.");

            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<ProjectAttribute>.Fail(ServiceStatus.NotFound404, "Project not found.");

            var attribute = await _repo.GetAttributeByPuid(puid);
            if (attribute == null || attribute.Project_Id != project.Project_Id) return ServiceResult<ProjectAttribute>.Fail(ServiceStatus.NotFound404, "Attribute not found.");

            var existingAttributes = await _repo.GetAttributesByProjectId(project.Project_Id);
            if (existingAttributes.Any(p => p.Name == name && p.Attribute_Id != attribute.Attribute_Id)) return ServiceResult<ProjectAttribute>.Fail(ServiceStatus.Conflict409, "Attribute name already exists for this project.");

            name = TruncateHelper.TruncateString(name, 255);
            description = TruncateHelper.TruncateStringNullable(description, 1000);
            unit = TruncateHelper.TruncateStringNullable(unit, 50);

            if (attribute.Name != name || attribute.Description != description || attribute.Unit != unit) attribute.Version += 1;

            attribute.Name = name;
            attribute.Description = description;
            attribute.Unit = unit;
            attribute.Last_Updated = DateTime.UtcNow;

            await _repo.UpdateAttribute(attribute);
            await UpdateProjectLastUpdated(project);
            return ServiceResult<ProjectAttribute>.SuccessResult(attribute);
        }

        public async Task<ServiceResult> DeleteAttribute(string projectPuid, string puid)
        {
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult.Fail(ServiceStatus.NotFound404, "Project not found.");

            var attribute = await _repo.GetAttributeByPuid(puid);
            if (attribute == null || attribute.Project_Id != project.Project_Id) return ServiceResult.Fail(ServiceStatus.NotFound404, "Attribute not found.");

            var isDeleted = await _repo.DeleteAttribute(attribute.Attribute_Id);
            if (!isDeleted) return ServiceResult.Fail(ServiceStatus.InternalServerError500, "Failed to delete attribute.");

            await UpdateProjectLastUpdated(project);
            return ServiceResult.SuccessResult(ServiceStatus.NoContent204);
        }

        private async Task UpdateProjectLastUpdated(Project project)
        {
            project.Last_Updated = DateTime.UtcNow;
            await _projectRepo.UpdateProject(project);
        }
    }
}
