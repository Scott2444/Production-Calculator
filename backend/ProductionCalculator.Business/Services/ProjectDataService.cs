using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Business.Services
{
    public class ProjectDataService  : IProjectDataService
    {
        private readonly IProductRepository _productRepo;
        private readonly IRecipeRepository _recipeRepo;
        private readonly IRecipeProductRepository _recipeProductRepo;
        private readonly IMachineRepository _machineRepo;
        private readonly IMachineRecipeRepository _machineRecipeRepo;
        private readonly IModifierRepository _modifierRepo;

        public ProjectDataService(
            IProductRepository productRepo,
            IRecipeRepository recipeRepo,
            IRecipeProductRepository recipeProductRepo,
            IMachineRepository machineRepo,
            IMachineRecipeRepository machineRecipeRepo,
            IModifierRepository modifierRepo)
        {
            _productRepo = productRepo;
            _recipeRepo = recipeRepo;
            _recipeProductRepo = recipeProductRepo;
            _machineRepo = machineRepo;
            _machineRecipeRepo = machineRecipeRepo;
            _modifierRepo = modifierRepo;
        }

        /// <summary>
        /// Gets all project objects for the specified project ID.
        /// Useful for workflow solving.
        /// </summary>
        /// <param name="projectId">ID of project to retrieve from</param>
        /// <returns>Aggregation of all project-related objects for the specified project ID.</returns>
        public async Task<ProjectObjects> GetProjectObjects(int projectId)
        {
            var products = await _productRepo.GetProductsByProjectId(projectId);
            var recipes = await _recipeRepo.GetByProjectId(projectId);
            var recipeProducts = new List<RecipeProduct>();
            foreach (var recipe in recipes)
            {
                var rProducts = await _recipeProductRepo.GetByRecipeId(recipe.Recipe_Id);
                recipeProducts.AddRange(rProducts);
            }
            var machines = await _machineRepo.GetMachinesByProjectId(projectId);
            var machineRecipes = new List<MachineRecipe>();
            foreach (var machine in machines)
            {
                var mRecipes = await _machineRecipeRepo.GetByMachineId(machine.Machine_Id);
                machineRecipes.AddRange(mRecipes);
            }
            var modifiers = await _modifierRepo.GetModifiersByProjectId(projectId);
            return new ProjectObjects
            {
                Products = products,
                Recipes = recipes,
                RecipeProducts = recipeProducts,
                Machines = machines,
                MachineRecipes = machineRecipes,
                Modifiers = modifiers
            };
        }
    }
}
