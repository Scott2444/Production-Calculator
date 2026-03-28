using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Helpers;

namespace ProductionCalculator.Business.Services
{
    public class ModifierService : IModifierService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IProjectRepository _projectRepo;
        private readonly IModifierRepository _repo;
        private readonly IModifierAttributeRepository _modifierAttributeRepo;
        private readonly IAttributeRepository _attributeRepo;
    
        public ModifierService(
            ICurrentUserService currentUser,
            IProjectRepository projectRepo,
            IModifierRepository repo,
            IModifierAttributeRepository modifierAttributeRepo,
            IAttributeRepository attributeRepo)
        {
            _currentUser = currentUser;
            _projectRepo = projectRepo;
            _repo = repo;
            _modifierAttributeRepo = modifierAttributeRepo;
            _attributeRepo = attributeRepo;
        }
        public async Task<ServiceResult<ModifierResponse>> AddModifier(string projectPuid, string name, string? description, double flatBonus, double percentBonus, double multiplicativeBonus, double inputPercent = 1.0, double outputPercent = 1.0, List<ModifierAttributeRequest>? attributes = null)
        {
            attributes ??= [];
            if (string.IsNullOrWhiteSpace(name)) return ServiceResult<ModifierResponse>.Fail(ServiceStatus.BadRequest400, "Modifier name is required.");

            // Get projectId from projectPuid
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<ModifierResponse>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Check if name already exists for this project
            var existingModifiers = await _repo.GetModifiersByProjectId(project.Project_Id);
            if (existingModifiers.Any(p => p.Name == name)) return ServiceResult<ModifierResponse>.Fail(ServiceStatus.Conflict409, "Modifier name already exists for this project.");

            var validatedAttributes = await ValidateAttributes(attributes, project.Project_Id);
            if (validatedAttributes.error != null)
            {
                return ServiceResult<ModifierResponse>.Fail(ServiceStatus.BadRequest400, validatedAttributes.error);
            }

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
                Flat_Bonus = flatBonus,
                Percent_Bonus = percentBonus,
                Multiplicative_Bonus = multiplicativeBonus,
                Input_Percent = inputPercent,
                Output_Percent = outputPercent,
                Version = 1,
                Created_At = DateTime.UtcNow,
                Last_Updated = DateTime.UtcNow
            };

            await _repo.AddModifier(modifier);

            var modifierAttributes = validatedAttributes.attributes.Select(attribute => new ModifierAttribute
            {
                Modifier_Attribute_Id = 0,
                Modifier_Id = modifier.Modifier_Id,
                Attribute_Id = attribute.attribute.Attribute_Id,
                Flat_Bonus = attribute.flatBonus,
                Percent_Bonus = attribute.percentBonus,
                Multiplicative_Bonus = attribute.multiplicativeBonus,
                Version = 1,
                Created_At = DateTime.UtcNow,
                Last_Updated = DateTime.UtcNow
            }).ToList();
            await _modifierAttributeRepo.AddModifierAttributes(modifierAttributes);

            await UpdateProjectLastUpdated(project);
            var modifierResponse = await BuildModifierResponse(modifier);
            return ServiceResult<ModifierResponse>.SuccessResult(modifierResponse, ServiceStatus.Created201);
        }
        public async Task<ServiceResult<ModifierResponse>> UpdateModifier(string projectPuid, string puid, string? name, string? description, double flatBonus, double percentBonus, double multiplicativeBonus, double inputPercent = 1.0, double outputPercent = 1.0, List<ModifierAttributeRequest>? attributes = null)
        {
            attributes ??= [];
            if (string.IsNullOrWhiteSpace(name)) return ServiceResult<ModifierResponse>.Fail(ServiceStatus.BadRequest400, "Modifier name is required.");

            // Get projectId from projectPuid
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<ModifierResponse>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Check if machine exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
            var modifier = await _repo.GetModifierByPuid(puid);
            if (modifier == null || modifier.Project_Id != project.Project_Id) return ServiceResult<ModifierResponse>.Fail(ServiceStatus.NotFound404, "Modifier not found.");

            // Check if name already exists for this project
            var existingModifiers = await _repo.GetModifiersByProjectId(project.Project_Id);
            if (existingModifiers.Any(p => p.Name == name && p.Puid != puid)) return ServiceResult<ModifierResponse>.Fail(ServiceStatus.Conflict409, "Modifier name already exists for this project.");

            var validatedAttributes = await ValidateAttributes(attributes, project.Project_Id);
            if (validatedAttributes.error != null)
            {
                return ServiceResult<ModifierResponse>.Fail(ServiceStatus.BadRequest400, validatedAttributes.error);
            }

            // Limit string lengths
            name = TruncateHelper.TruncateString(name, 255);
            description = TruncateHelper.TruncateStringNullable(description, 1000);

            modifier.Name = name;
            modifier.Description = description ?? string.Empty;
            if (modifier.Flat_Bonus != flatBonus || 
                modifier.Percent_Bonus != percentBonus || 
                modifier.Multiplicative_Bonus != multiplicativeBonus ||
                modifier.Input_Percent != inputPercent ||
                modifier.Output_Percent != outputPercent) 
                { modifier.Version += 1; } // Only increment version if tangible change
            modifier.Flat_Bonus = flatBonus;
            modifier.Percent_Bonus = percentBonus;
            modifier.Multiplicative_Bonus = multiplicativeBonus;
            modifier.Input_Percent = inputPercent;
            modifier.Output_Percent = outputPercent;
            modifier.Last_Updated = DateTime.UtcNow;

            await _repo.UpdateModifier(modifier);

            var existingModifierAttributes = (await _modifierAttributeRepo.GetByModifierId(modifier.Modifier_Id)).ToList();
            var modifierAttributesToAdd = validatedAttributes.attributes
                .Where(a => !existingModifierAttributes.Any(ma => ma.Attribute_Id == a.attribute.Attribute_Id))
                .Select(a => new ModifierAttribute
                {
                    Modifier_Attribute_Id = 0,
                    Modifier_Id = modifier.Modifier_Id,
                    Attribute_Id = a.attribute.Attribute_Id,
                    Flat_Bonus = a.flatBonus,
                    Percent_Bonus = a.percentBonus,
                    Multiplicative_Bonus = a.multiplicativeBonus,
                    Version = 1,
                    Created_At = DateTime.UtcNow,
                    Last_Updated = DateTime.UtcNow
                })
                .ToList();
            var modifierAttributesToUpdate = existingModifierAttributes
                .Where(ma => validatedAttributes.attributes.Any(a => a.attribute.Attribute_Id == ma.Attribute_Id))
                .ToList();
            foreach (var modifierAttribute in modifierAttributesToUpdate)
            {
                var incomingAttribute = validatedAttributes.attributes.First(a => a.attribute.Attribute_Id == modifierAttribute.Attribute_Id);
                modifierAttribute.Flat_Bonus = incomingAttribute.flatBonus;
                modifierAttribute.Percent_Bonus = incomingAttribute.percentBonus;
                modifierAttribute.Multiplicative_Bonus = incomingAttribute.multiplicativeBonus;
                modifierAttribute.Version += 1;
                modifierAttribute.Last_Updated = DateTime.UtcNow;
            }
            var modifierAttributesToDelete = existingModifierAttributes
                .Where(ma => !validatedAttributes.attributes.Any(a => a.attribute.Attribute_Id == ma.Attribute_Id))
                .ToList();

            await _modifierAttributeRepo.AddModifierAttributes(modifierAttributesToAdd);
            await _modifierAttributeRepo.UpdateModifierAttributes(modifierAttributesToUpdate);
            await _modifierAttributeRepo.DeleteModifierAttributes(modifierAttributesToDelete.Select(ma => ma.Modifier_Attribute_Id));

            await UpdateProjectLastUpdated(project);
            var modifierResponse = await BuildModifierResponse(modifier);
            return ServiceResult<ModifierResponse>.SuccessResult(modifierResponse, ServiceStatus.Ok200);
        }
        public async Task<ServiceResult<ModifierResponse>> GetModifierByPuid(string projectPuid, string puid)
        {
            // Get projectId from projectPuid
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<ModifierResponse>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Redirect aliased project to canonical project PUID
            if (!string.IsNullOrWhiteSpace(project.Alias_Project_Puid))
            {
                return ServiceResult<ModifierResponse>.Redirection(ServiceStatus.SeeOther303, $"/projects/{project.Alias_Project_Puid}/modifiers/{puid}");
            }

            // Check if modifier exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
            var modifier = await _repo.GetModifierByPuid(puid);
            if (modifier == null || modifier.Project_Id != project.Project_Id) return ServiceResult<ModifierResponse>.Fail(ServiceStatus.NotFound404, "Modifier not found.");

            var modifierResponse = await BuildModifierResponse(modifier);
            return ServiceResult<ModifierResponse>.SuccessResult(modifierResponse);
        }
        public async Task<ServiceResult<List<ModifierResponse>>> GetModifiersByProjectPuid(string projectPuid)
        {
            // Authorization already checked if project exists, otherwise they would not have access to it
            // i.e. this should never fail
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<List<ModifierResponse>>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Redirect aliased project to canonical project PUID
            if (!string.IsNullOrWhiteSpace(project.Alias_Project_Puid))
            {
                return ServiceResult<List<ModifierResponse>>.Redirection(ServiceStatus.SeeOther303, $"/projects/{project.Alias_Project_Puid}/modifiers");
            }

            var modifiers = await _repo.GetModifiersByProjectId(project.Project_Id);
            var modifierResponses = new List<ModifierResponse>();
            foreach (var modifier in modifiers)
            {
                modifierResponses.Add(await BuildModifierResponse(modifier));
            }

            return ServiceResult<List<ModifierResponse>>.SuccessResult(modifierResponses);
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

            await UpdateProjectLastUpdated(project);
            return ServiceResult.SuccessResult(ServiceStatus.NoContent204);
        }

        private async Task UpdateProjectLastUpdated(Project project)
        {
            project.Last_Updated = DateTime.UtcNow;
            await _projectRepo.UpdateProject(project);
        }

        private async Task<ModifierResponse> BuildModifierResponse(Modifier modifier)
        {
            var modifierAttributes = (await _modifierAttributeRepo.GetByModifierId(modifier.Modifier_Id)).ToList();
            var attributeResponses = new List<ModifierAttributeResponse>();
            foreach (var modifierAttribute in modifierAttributes)
            {
                var attribute = await _attributeRepo.GetAttributeById(modifierAttribute.Attribute_Id);
                if (attribute == null)
                {
                    continue;
                }

                attributeResponses.Add(new ModifierAttributeResponse
                {
                    Puid = attribute.Puid,
                    FlatBonus = modifierAttribute.Flat_Bonus,
                    PercentBonus = modifierAttribute.Percent_Bonus,
                    MultiplicativeBonus = modifierAttribute.Multiplicative_Bonus,
                    CreatedAt = modifierAttribute.Created_At,
                    UpdatedAt = modifierAttribute.Last_Updated
                });
            }

            return new ModifierResponse
            {
                Puid = modifier.Puid,
                Name = modifier.Name,
                Description = modifier.Description,
                FlatBonus = modifier.Flat_Bonus,
                PercentBonus = modifier.Percent_Bonus,
                MultiplicativeBonus = modifier.Multiplicative_Bonus,
                InputPercent = modifier.Input_Percent,
                OutputPercent = modifier.Output_Percent,
                Attributes = attributeResponses,
                CreatedAt = modifier.Created_At,
                UpdatedAt = modifier.Last_Updated
            };
        }

        private async Task<(List<(ProjectAttribute attribute, double flatBonus, double percentBonus, double multiplicativeBonus)> attributes, string? error)> ValidateAttributes(List<ModifierAttributeRequest> attributes, int projectId)
        {
            var uniqueAttributes = attributes
                .GroupBy(a => a.Puid)
                .Select(g => g.First())
                .ToList();

            if (uniqueAttributes.Count != attributes.Count)
            {
                return ([], "Duplicate attribute PUIDs are not allowed.");
            }

            var validatedAttributes = new List<(ProjectAttribute attribute, double flatBonus, double percentBonus, double multiplicativeBonus)>();
            foreach (var attributeRelation in uniqueAttributes)
            {
                var attribute = await _attributeRepo.GetAttributeByPuid(attributeRelation.Puid);
                if (attribute == null || attribute.Project_Id != projectId)
                {
                    return ([], $"Invalid attribute PUID: {attributeRelation.Puid}");
                }

                validatedAttributes.Add((attribute, attributeRelation.FlatBonus, attributeRelation.PercentBonus, attributeRelation.MultiplicativeBonus));
            }

            return (validatedAttributes, null);
        }
    }
}
