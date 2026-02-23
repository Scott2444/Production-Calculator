using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Helpers;

namespace ProductionCalculator.Business.Services
{
    public class MachineService : IMachineService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IMachineRepository _repo;
        private readonly IMachineRecipeRepository _machineRecipeRepo;
        private readonly IMachineAttributeRepository _machineAttributeRepo;
        private readonly IRecipeRepository _recipeRepo;
        private readonly IAttributeRepository _attributeRepo;
        private readonly IProjectRepository _projectRepo;
        public MachineService(
            ICurrentUserService currentUser, 
            IMachineRepository repo, 
            IMachineRecipeRepository machineRecipeRepo, 
            IMachineAttributeRepository machineAttributeRepo,
            IRecipeRepository recipeRepo,
            IAttributeRepository attributeRepo,
            IProjectRepository projectRepo
            ) 
        { 
            _currentUser = currentUser; 
            _repo = repo;
            _machineRecipeRepo = machineRecipeRepo;
            _machineAttributeRepo = machineAttributeRepo;
            _recipeRepo = recipeRepo;
            _attributeRepo = attributeRepo;
            _projectRepo = projectRepo;
        }

        public async Task<ServiceResult<MachineResponse>> AddMachine(string projectPuid, string name, string? description, double baseSpeed, List<string> recipePuids, List<AttributeRateExchange>? attributes = null)
        {
            attributes ??= [];
            if (string.IsNullOrWhiteSpace(name)) return ServiceResult<MachineResponse>.Fail(ServiceStatus.BadRequest400, "Machine name is required.");

            // Get projectId from projectPuid
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<MachineResponse>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Check if name already exists for this project
            var existingMachines = await _repo.GetMachinesByProjectId(project.Project_Id);
            if (existingMachines.Any(p => p.Name == name)) return ServiceResult<MachineResponse>.Fail(ServiceStatus.Conflict409, "Machine name already exists for this project.");

            // Validate base_speed
            if (baseSpeed <= 0) return ServiceResult<MachineResponse>.Fail(ServiceStatus.BadRequest400, "Base speed must be greater than zero.");

            // Validate recipePuids
            recipePuids = recipePuids.Distinct().ToList();
            var validRecipes = new List<Recipe>();
            foreach (var recipePuid in recipePuids)
            {
                var recipe = await _recipeRepo.GetByPuid(recipePuid);
                if (recipe == null || recipe.Project_Id != project.Project_Id)
                {
                    return ServiceResult<MachineResponse>.Fail(ServiceStatus.BadRequest400, $"Invalid recipe PUID: {recipePuid}");
                }
                validRecipes.Add(recipe);
            }

            var validatedAttributes = await ValidateAttributes(attributes, project.Project_Id);
            if (validatedAttributes.error != null)
            {
                return ServiceResult<MachineResponse>.Fail(ServiceStatus.BadRequest400, validatedAttributes.error);
            }

            // Limit string lengths
            name = TruncateHelper.TruncateString(name, 255);
            description = TruncateHelper.TruncateStringNullable(description, 1000);

            var puid = PuidHelper.GeneratePuid();

            // Create machine and save to database
            var machine = new Machine
            {
                Machine_Id = 0,
                Project_Id = project.Project_Id,
                Puid = puid,
                Name = name,
                Description = description ?? string.Empty,
                Base_Speed = baseSpeed,
                Version = 1,
                Created_At = DateTime.UtcNow,
                Last_Updated = DateTime.UtcNow
            };
            await _repo.AddMachine(machine);

            // Create machine_recipe relations and save to database
            var machineRecipe = recipePuids.Select(rp => new MachineRecipe
            {
                Machine_Recipe_Id = 0,
                Machine_Id = machine.Machine_Id,
                Recipe_Id = validRecipes.First(r => rp == r.Puid).Recipe_Id
            }).ToList();
            await _machineRecipeRepo.AddMachineRecipes(machineRecipe);

            var machineAttributes = validatedAttributes.attributes.Select(attribute => new MachineAttribute
            {
                Machine_Attribute_Id = 0,
                Machine_Id = machine.Machine_Id,
                Attribute_Id = attribute.attribute.Attribute_Id,
                Rate = attribute.rate,
                Version = 1,
                Created_At = DateTime.UtcNow,
                Last_Updated = DateTime.UtcNow
            }).ToList();
            await _machineAttributeRepo.AddMachineAttributes(machineAttributes);

            await UpdateProjectLastUpdated(project);

            // Convert to MachineResponse
            var machineResponse = new MachineResponse
            {
                Puid = machine.Puid,
                Name = machine.Name,
                Description = machine.Description,
                BaseSpeed = machine.Base_Speed,
                RecipePuids = validRecipes.Select(r => r.Puid).ToList(),
                Attributes = attributes,
                CreatedAt = machine.Created_At,
                UpdatedAt = machine.Last_Updated
            };

            return ServiceResult<MachineResponse>.SuccessResult(machineResponse, ServiceStatus.Created201);
        }
        public async Task<ServiceResult<MachineResponse>> UpdateMachine(string projectPuid, string puid, string? name, string? description, double baseSpeed, List<string> recipePuids, List<AttributeRateExchange>? attributes = null)
        {
            attributes ??= [];
            if (string.IsNullOrWhiteSpace(name)) return ServiceResult<MachineResponse>.Fail(ServiceStatus.BadRequest400, "Machine name is required.");

            // Get projectId from projectPuid
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<MachineResponse>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Check if machine exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
            var machine = await _repo.GetMachineByPuid(puid);
            if (machine == null || machine.Project_Id != project.Project_Id) return ServiceResult<MachineResponse>.Fail(ServiceStatus.NotFound404, "Machine not found.");

            // Check if name already exists for this project
            var existingMachines = await _repo.GetMachinesByProjectId(project.Project_Id);
            if (existingMachines.Any(p => p.Name == name && p.Puid != puid)) return ServiceResult<MachineResponse>.Fail(ServiceStatus.Conflict409, "Machine name already exists for this project.");

            // Validate base_speed
            if (baseSpeed <= 0) return ServiceResult<MachineResponse>.Fail(ServiceStatus.BadRequest400, "Base speed must be greater than zero.");

            // Validate recipePuids
            recipePuids = recipePuids.Distinct().ToList();
            var validRecipes = new List<Recipe>();
            foreach (var recipePuid in recipePuids)
            {
                var recipe = await _recipeRepo.GetByPuid(recipePuid);
                if (recipe == null || recipe.Project_Id != project.Project_Id)
                {
                    return ServiceResult<MachineResponse>.Fail(ServiceStatus.BadRequest400, $"Invalid recipe PUID: {recipePuid}");
                }
                validRecipes.Add(recipe);
            }

            var validatedAttributes = await ValidateAttributes(attributes, project.Project_Id);
            if (validatedAttributes.error != null)
            {
                return ServiceResult<MachineResponse>.Fail(ServiceStatus.BadRequest400, validatedAttributes.error);
            }

            // Limit string lengths
            name = TruncateHelper.TruncateString(name, 255);
            description = TruncateHelper.TruncateStringNullable(description, 1000);

            // Update machine and save to database
            machine.Name = name;
            machine.Description = description ?? string.Empty;
            if (machine.Base_Speed != baseSpeed) machine.Version += 1; // Only increment version if tangible change
            machine.Base_Speed = baseSpeed;
            machine.Last_Updated = DateTime.UtcNow;
            await _repo.UpdateMachine(machine);

            // Update machine_recipe relations
            // Find relations to add and remove
            var existingMachineRecipes = (await _machineRecipeRepo.GetByMachineId(machine.Machine_Id)).ToList();
            var machineRecipesToAdd = validRecipes
                .Where(r => !existingMachineRecipes.Any(mr => mr.Recipe_Id == r.Recipe_Id))
                .Select(r => new MachineRecipe
                {
                    Machine_Recipe_Id = 0,
                    Machine_Id = machine.Machine_Id,
                    Recipe_Id = r.Recipe_Id
                })
                .ToList();
            var machineRecipesToRemove = existingMachineRecipes
                .Where(mr => !validRecipes.Any(r => r.Recipe_Id == mr.Recipe_Id))
                .ToList();
            
            await _machineRecipeRepo.AddMachineRecipes(machineRecipesToAdd);
            await _machineRecipeRepo.DeleteMachineRecipes(machineRecipesToRemove.Select(mr => mr.Machine_Recipe_Id));

            var existingMachineAttributes = (await _machineAttributeRepo.GetByMachineId(machine.Machine_Id)).ToList();
            var machineAttributesToAdd = validatedAttributes.attributes
                .Where(a => !existingMachineAttributes.Any(ma => ma.Attribute_Id == a.attribute.Attribute_Id))
                .Select(a => new MachineAttribute
                {
                    Machine_Attribute_Id = 0,
                    Machine_Id = machine.Machine_Id,
                    Attribute_Id = a.attribute.Attribute_Id,
                    Rate = a.rate,
                    Version = 1,
                    Created_At = DateTime.UtcNow,
                    Last_Updated = DateTime.UtcNow
                })
                .ToList();
            var machineAttributesToUpdate = existingMachineAttributes
                .Where(ma => validatedAttributes.attributes.Any(a => a.attribute.Attribute_Id == ma.Attribute_Id))
                .ToList();
            foreach (var machineAttribute in machineAttributesToUpdate)
            {
                var incomingAttribute = validatedAttributes.attributes.First(a => a.attribute.Attribute_Id == machineAttribute.Attribute_Id);
                machineAttribute.Rate = incomingAttribute.rate;
                machineAttribute.Version += 1;
                machineAttribute.Last_Updated = DateTime.UtcNow;
            }
            var machineAttributesToDelete = existingMachineAttributes
                .Where(ma => !validatedAttributes.attributes.Any(a => a.attribute.Attribute_Id == ma.Attribute_Id))
                .ToList();

            await _machineAttributeRepo.AddMachineAttributes(machineAttributesToAdd);
            await _machineAttributeRepo.UpdateMachineAttributes(machineAttributesToUpdate);
            await _machineAttributeRepo.DeleteMachineAttributes(machineAttributesToDelete.Select(ma => ma.Machine_Attribute_Id));

            await UpdateProjectLastUpdated(project);

            // Convert to MachineResponse
            var machineResponse = new MachineResponse
            {
                Puid = machine.Puid,
                Name = machine.Name,
                Description = machine.Description,
                BaseSpeed = machine.Base_Speed,
                RecipePuids = validRecipes.Select(r => r.Puid).ToList(),
                Attributes = attributes,
                CreatedAt = machine.Created_At,
                UpdatedAt = machine.Last_Updated
            };
            return ServiceResult<MachineResponse>.SuccessResult(machineResponse);
        }
        public async Task<ServiceResult<MachineResponse>> GetMachineByPuid(string projectPuid, string puid)
        {
            // Get projectId from projectPuid
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<MachineResponse>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Check if machine exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
            var machine = await _repo.GetMachineByPuid(puid);
            if (machine == null || machine.Project_Id != project.Project_Id) return ServiceResult<MachineResponse>.Fail(ServiceStatus.NotFound404, "Machine not found.");

            // Get machine_recipes
            var machineRecipes = (await _machineRecipeRepo.GetByMachineId(machine.Machine_Id)).ToList();
            var machineAttributes = (await _machineAttributeRepo.GetByMachineId(machine.Machine_Id)).ToList();

            // Get recipes
            var recipes = new List<Recipe>();
            foreach (var mr in machineRecipes)
            {
                var recipe = await _recipeRepo.GetById(mr.Recipe_Id);
                if (recipe != null) recipes.Add(recipe);
            }

            var attributes = new List<AttributeRateExchange>();
            foreach (var machineAttribute in machineAttributes)
            {
                var attribute = await _attributeRepo.GetAttributeById(machineAttribute.Attribute_Id);
                if (attribute != null)
                {
                    attributes.Add(new AttributeRateExchange
                    {
                        Puid = attribute.Puid,
                        Rate = machineAttribute.Rate
                    });
                }
            }

            // Convert to MachineResponse
            var machineResponse = new MachineResponse
            {
                Puid = machine.Puid,
                Name = machine.Name,
                Description = machine.Description,
                BaseSpeed = machine.Base_Speed,
                RecipePuids = recipes.Select(r => r.Puid).ToList(),
                Attributes = attributes,
                CreatedAt = machine.Created_At,
                UpdatedAt = machine.Last_Updated
            };

            return ServiceResult<MachineResponse>.SuccessResult(machineResponse);
        }
        public async Task<ServiceResult<List<MachineResponse>>> GetMachinesByProjectPuid(string projectPuid)
        {
            // Get projectId from projectPuid
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<List<MachineResponse>>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Check if machine exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
            var machines = await _repo.GetMachinesByProjectId(project.Project_Id);

            // Get machine_recipes
            var machineRecipes = new List<MachineRecipe>();
            var machineAttributes = new List<MachineAttribute>();
            foreach (var machine in machines)
            {
                machineRecipes.AddRange(await _machineRecipeRepo.GetByMachineId(machine.Machine_Id));
                machineAttributes.AddRange(await _machineAttributeRepo.GetByMachineId(machine.Machine_Id));
            }

            // Get recipes
            var recipes = new List<Recipe>();
            foreach (var mr in machineRecipes)
            {
                var recipe = await _recipeRepo.GetById(mr.Recipe_Id);
                if (recipe != null) recipes.Add(recipe);
            }

            // Convert to MachineResponse
            var machineResponses = new List<MachineResponse>();
            foreach (var machine in machines)
            {
                var associatedRecipes = machineRecipes
                    .Where(mr => mr.Machine_Id == machine.Machine_Id)
                    .Select(mr => recipes.FirstOrDefault(r => r.Recipe_Id == mr.Recipe_Id))
                    .Where(r => r != null)
                    .ToList();

                var associatedAttributes = machineAttributes
                    .Where(ma => ma.Machine_Id == machine.Machine_Id)
                    .ToList();
                var attributeResponses = new List<AttributeRateExchange>();
                foreach (var associatedAttribute in associatedAttributes)
                {
                    var attribute = await _attributeRepo.GetAttributeById(associatedAttribute.Attribute_Id);
                    if (attribute != null)
                    {
                        attributeResponses.Add(new AttributeRateExchange
                        {
                            Puid = attribute.Puid,
                            Rate = associatedAttribute.Rate
                        });
                    }
                }

                var machineResponse = new MachineResponse
                {
                    Puid = machine.Puid,
                    Name = machine.Name,
                    Description = machine.Description,
                    BaseSpeed = machine.Base_Speed,
                    RecipePuids = associatedRecipes.Select(r => r!.Puid).ToList(),
                    Attributes = attributeResponses,
                    CreatedAt = machine.Created_At,
                    UpdatedAt = machine.Last_Updated
                };
                machineResponses.Add(machineResponse);
            }

            return ServiceResult<List<MachineResponse>>.SuccessResult(machineResponses);
        }
        public async Task<ServiceResult> DeleteMachine(string projectPuid, string puid)
        {
            // Get projectId from projectPuid
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Check if machine exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
            var machine = await _repo.GetMachineByPuid(puid);
            if (machine == null || machine.Project_Id != project.Project_Id) return ServiceResult.Fail(ServiceStatus.NotFound404, "Machine not found.");
            
            var isDeleted = await _repo.DeleteMachine(machine.Machine_Id);
            if (!isDeleted) return ServiceResult.Fail(ServiceStatus.InternalServerError500, "Failed to delete machine.");
            await UpdateProjectLastUpdated(project);
            return ServiceResult.SuccessResult(ServiceStatus.NoContent204);
        }

        private async Task UpdateProjectLastUpdated(Project project)
        {
            project.Last_Updated = DateTime.UtcNow;
            await _projectRepo.UpdateProject(project);
        }

        private async Task<(List<(ProjectAttribute attribute, double rate)> attributes, string? error)> ValidateAttributes(List<AttributeRateExchange> attributes, int projectId)
        {
            var uniqueAttributes = attributes
                .GroupBy(a => a.Puid)
                .Select(g => g.First())
                .ToList();

            if (uniqueAttributes.Count != attributes.Count)
            {
                return ([], "Duplicate attribute PUIDs are not allowed.");
            }

            var validatedAttributes = new List<(ProjectAttribute attribute, double rate)>();
            foreach (var attributeRelation in uniqueAttributes)
            {
                if (attributeRelation.Rate <= 0)
                {
                    return ([], $"Attribute rate must be greater than zero for {attributeRelation.Puid}.");
                }

                var attribute = await _attributeRepo.GetAttributeByPuid(attributeRelation.Puid);
                if (attribute == null || attribute.Project_Id != projectId)
                {
                    return ([], $"Invalid attribute PUID: {attributeRelation.Puid}");
                }

                validatedAttributes.Add((attribute, attributeRelation.Rate));
            }

            return (validatedAttributes, null);
        }
    }
}