using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Business.Services
{
    public class ProjectDataService  : IProjectDataService
    {
        private readonly IProductRepository _productRepo;
        private readonly IAttributeRepository _attributeRepo;
        private readonly IRecipeRepository _recipeRepo;
        private readonly IRecipeProductRepository _recipeProductRepo;
        private readonly IRecipeAttributeRepository _recipeAttributeRepo;
        private readonly IMachineRepository _machineRepo;
        private readonly IMachineRecipeRepository _machineRecipeRepo;
        private readonly IMachineAttributeRepository _machineAttributeRepo;
        private readonly IModifierRepository _modifierRepo;
        private readonly IModifierAttributeRepository _modifierAttributeRepo;
        private readonly IProjectRepository _projectRepo;

        public ProjectDataService(
            IProductRepository productRepo,
            IAttributeRepository attributeRepo,
            IRecipeRepository recipeRepo,
            IRecipeProductRepository recipeProductRepo,
            IRecipeAttributeRepository recipeAttributeRepo,
            IMachineRepository machineRepo,
            IMachineRecipeRepository machineRecipeRepo,
            IMachineAttributeRepository machineAttributeRepo,
            IModifierRepository modifierRepo,
            IModifierAttributeRepository modifierAttributeRepo,
            IProjectRepository projectRepo)
        {
            _productRepo = productRepo;
            _attributeRepo = attributeRepo;
            _recipeRepo = recipeRepo;
            _recipeProductRepo = recipeProductRepo;
            _recipeAttributeRepo = recipeAttributeRepo;
            _machineRepo = machineRepo;
            _machineRecipeRepo = machineRecipeRepo;
            _machineAttributeRepo = machineAttributeRepo;
            _modifierRepo = modifierRepo;
            _modifierAttributeRepo = modifierAttributeRepo;
            _projectRepo = projectRepo;
        }

        /// <summary>
        /// Gets all project objects for the specified project ID.
        /// Useful for workflow solving.
        /// </summary>
        /// <param name="projectId">ID of project to retrieve from</param>
        /// <returns>Aggregation of all project-related objects for the specified project ID.</returns>
        public async Task<ProjectObjects> GetProjectObjects(int projectId)
        {
            var componentProjectId = await ResolveComponentProjectId(projectId);
            var products = await _productRepo.GetProductsByProjectId(componentProjectId);
            var attributes = await _attributeRepo.GetAttributesByProjectId(componentProjectId);
            var recipes = await _recipeRepo.GetByProjectId(componentProjectId);
            var recipeProducts = new List<RecipeProduct>();
            var recipeAttributes = new List<RecipeAttribute>();
            foreach (var recipe in recipes)
            {
                var rProducts = await _recipeProductRepo.GetByRecipeId(recipe.Recipe_Id);
                recipeProducts.AddRange(rProducts);

                var rAttributes = await _recipeAttributeRepo.GetByRecipeId(recipe.Recipe_Id);
                recipeAttributes.AddRange(rAttributes);
            }
            var machines = await _machineRepo.GetMachinesByProjectId(componentProjectId);
            var machineRecipes = new List<MachineRecipe>();
            var machineAttributes = new List<MachineAttribute>();
            foreach (var machine in machines)
            {
                var mRecipes = await _machineRecipeRepo.GetByMachineId(machine.Machine_Id);
                machineRecipes.AddRange(mRecipes);

                var mAttributes = await _machineAttributeRepo.GetByMachineId(machine.Machine_Id);
                machineAttributes.AddRange(mAttributes);
            }
            var modifiers = await _modifierRepo.GetModifiersByProjectId(componentProjectId);
            var modifierAttributes = new List<ModifierAttribute>();
            foreach (var modifier in modifiers)
            {
                var mAttributes = await _modifierAttributeRepo.GetByModifierId(modifier.Modifier_Id);
                modifierAttributes.AddRange(mAttributes);
            }
            return new ProjectObjects
            {
                Products = products,
                Attributes = attributes,
                Recipes = recipes,
                RecipeProducts = recipeProducts,
                RecipeAttributes = recipeAttributes,
                Machines = machines,
                MachineRecipes = machineRecipes,
                MachineAttributes = machineAttributes,
                Modifiers = modifiers,
                ModifierAttributes = modifierAttributes
            };
        }

        private async Task<int> ResolveComponentProjectId(int projectId)
        {
            var project = await _projectRepo.GetProjectById(projectId);
            if (project == null || string.IsNullOrWhiteSpace(project.Alias_Project_Puid))
                return projectId;

            var sourceProject = await _projectRepo.GetProjectByPuid(project.Alias_Project_Puid);
            if (sourceProject == null)
                return projectId;

            // Security: only follow alias when it still satisfies alias authorization rules
            if (sourceProject.User_Id != project.User_Id && !sourceProject.Is_Public)
                return projectId;

            return sourceProject.Project_Id;
        }
    }
}
