using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.APIModels;
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
    public class RecipeService : IRecipeService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IRecipeRepository _repo;
        private readonly IProductRepository _productRepo;
        private readonly IRecipeProductRepository _recipeProductRepo;
        private readonly IProjectRepository _projectRepo;
        public RecipeService(
            ICurrentUserService currentUser, 
            IProductRepository productRepo, 
            IRecipeRepository repo, 
            IRecipeProductRepository recipeProductRepo, 
            IProjectRepository projectRepo
            ) 
        { 
            _currentUser = currentUser; 
            _repo = repo;
            _productRepo = productRepo;
            _recipeProductRepo = recipeProductRepo;
            _projectRepo = projectRepo;
        }

        public async Task<ServiceResult<RecipeResponse>> AddRecipe(string projectPuid, string name, string? description, double baseCraftingTime, List<RecipeProductExchange> inputs, List<RecipeProductExchange> outputs)
        {
            if (string.IsNullOrWhiteSpace(name)) return ServiceResult<RecipeResponse>.Fail(ServiceStatus.BadRequest400, "Recipe name is required.");

            // Get products
            var cachedProducts = new List<Product>();
            await CacheProducts(inputs.Concat(outputs).ToList(), cachedProducts); // Avoids multiple db calls

            // Get projectId from projectPuid
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<RecipeResponse>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Check if name already exists for this project
            var existingRecipes = await _repo.GetByProjectId(project.Project_Id);
            if (existingRecipes.Any(p => p.Name == name)) return ServiceResult<RecipeResponse>.Fail(ServiceStatus.Conflict409, "Recipe name already exists for this project.");

            // Validate inputs and outputs
            var (areInputsValid, inputError) = CheckProducts(inputs, project.Project_Id, cachedProducts);
            if (!areInputsValid) return ServiceResult<RecipeResponse>.Fail(ServiceStatus.BadRequest400, inputError);

            var (areOutputsValid, outputError) = CheckProducts(outputs, project.Project_Id, cachedProducts);
            if (!areOutputsValid) return ServiceResult<RecipeResponse>.Fail(ServiceStatus.BadRequest400, outputError);

            // Check baseCraftingTime is positive
            if (baseCraftingTime <= 0) return ServiceResult<RecipeResponse>.Fail(ServiceStatus.BadRequest400, "Base crafting time must be positive.");

            // Limit string lengths
            name = TruncateHelper.TruncateString(name, 255);
            description = TruncateHelper.TruncateStringNullable(description, 1000);

            // Generate new PUID
            var puid = await PuidHelper.GenerateUniquePuidAsync(_repo.PuidExists);

            // Add recipe to DB
            var recipe = new Recipe
            {
                Recipe_Id = 0,
                Project_Id = project.Project_Id,
                Puid = puid,
                Name = name,
                Description = description ?? string.Empty,
                Base_Crafting_Time = baseCraftingTime,
                Version = 1,
                Created_At = DateTime.UtcNow,
                Last_Updated = DateTime.UtcNow
            };
            await _repo.AddRecipe(recipe);

            // Generate recipe products
            var recipeProducts = new List<RecipeProduct>();
            recipeProducts.AddRange(AssembleRecipeProducts(inputs, recipe.Recipe_Id, cachedProducts, isInput: true));
            recipeProducts.AddRange(AssembleRecipeProducts(outputs, recipe.Recipe_Id, cachedProducts, isInput: false));

            // Add recipe_products to DB
            await _recipeProductRepo.UpsertRecipeProducts(recipeProducts);

            // Convert to API model
           var recipeResponse = ConvertToApiModel(recipe, recipeProducts, cachedProducts);
           return ServiceResult<RecipeResponse>.SuccessResult(recipeResponse, ServiceStatus.Created201);
        }
        public async Task<ServiceResult<RecipeResponse>> UpdateRecipe(string projectPuid, string puid, string name, string? description, double baseCraftingTime, List<RecipeProductExchange> inputs, List<RecipeProductExchange> outputs)
        {
            if (string.IsNullOrWhiteSpace(name)) return ServiceResult<RecipeResponse>.Fail(ServiceStatus.BadRequest400, "Recipe name is required.");

            // Get products
            var cachedProducts = new List<Product>();
            await CacheProducts(inputs.Concat(outputs).ToList(), cachedProducts); // Avoids multiple db calls
            // Get projectId from projectPuid
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<RecipeResponse>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Check if recipe exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
            var recipe = await _repo.GetByPuid(puid);
            if (recipe == null || recipe.Project_Id != project.Project_Id) return ServiceResult<RecipeResponse>.Fail(ServiceStatus.NotFound404, "Recipe not found.");

            // Check if name already exists for this project
            var existingRecipes = await _repo.GetByProjectId(project.Project_Id);
            if (existingRecipes.Any(p => p.Name == name && p.Recipe_Id != recipe.Recipe_Id)) return ServiceResult<RecipeResponse>.Fail(ServiceStatus.Conflict409, "Recipe name already exists for this project.");

            // Validate inputs and outputs
            var (areInputsValid, inputError) = CheckProducts(inputs, project.Project_Id, cachedProducts);
            if (!areInputsValid) return ServiceResult<RecipeResponse>.Fail(ServiceStatus.BadRequest400, inputError);

            var (areOutputsValid, outputError) = CheckProducts(outputs, project.Project_Id, cachedProducts);
            if (!areOutputsValid) return ServiceResult<RecipeResponse>.Fail(ServiceStatus.BadRequest400, outputError);

            // Check baseCraftingTime is positive
            if (baseCraftingTime <= 0) return ServiceResult<RecipeResponse>.Fail(ServiceStatus.BadRequest400, "Base crafting time must be positive.");

            // Limit string lengths
            name = TruncateHelper.TruncateString(name, 255);
            description = TruncateHelper.TruncateStringNullable(description, 1000);

            // Handle recipe products upsert
            var existingRecipeProducts = await _recipeProductRepo.GetByRecipeId(recipe.Recipe_Id);
            var (toAdd, toUpdate, toDelete) = HandleRecipeProductUpdate(inputs, outputs, existingRecipeProducts, recipe.Recipe_Id, cachedProducts);

            await _recipeProductRepo.UpsertRecipeProducts(toAdd);
            await _recipeProductRepo.UpsertRecipeProducts(toUpdate);
            await _recipeProductRepo.DeleteRecipeProducts(toDelete.Select(rp => rp.Recipe_Product_Id));

            // Update recipe in DB
            recipe.Name = name;
            recipe.Description = description ?? string.Empty;
            if (recipe.Base_Crafting_Time != baseCraftingTime || toAdd.Any() || toUpdate.Any() || toDelete.Any()) recipe.Version += 1; // Only increment version if tangible change
            recipe.Base_Crafting_Time = baseCraftingTime;
            recipe.Last_Updated = DateTime.UtcNow;
            await _repo.UpdateRecipe(recipe);

            // Convert to API model
            var recipeResponse = ConvertToApiModel(recipe, toAdd.Concat(toUpdate), cachedProducts);
            return ServiceResult<RecipeResponse>.SuccessResult(recipeResponse);
        }
        public async Task<ServiceResult<RecipeResponse>> GetRecipeByPuid(string projectPuid, string puid)
        {
            // Get projectId from projectPuid
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<RecipeResponse>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Redirect aliased project to canonical project PUID
            if (!string.IsNullOrWhiteSpace(project.Alias_Project_Puid))
            {
                return ServiceResult<RecipeResponse>.Redirection(ServiceStatus.SeeOther303, $"/api/projects/{project.Alias_Project_Puid}/recipes/{puid}");
            }

            // Check if recipe exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
            var recipe = await _repo.GetByPuid(puid);
            if (recipe == null || recipe.Project_Id != project.Project_Id) return ServiceResult<RecipeResponse>.Fail(ServiceStatus.NotFound404, "Recipe not found.");

            // Get recipe products
            var recipeProducts = await _recipeProductRepo.GetByRecipeId(recipe.Recipe_Id);

            // Cache products
            var cachedProducts = new List<Product>();
            await CacheProducts(recipeProducts, cachedProducts);
            
            // Convert to API model
            var recipeResponse = ConvertToApiModel(recipe, recipeProducts, cachedProducts);
            return ServiceResult<RecipeResponse>.SuccessResult(recipeResponse);
        }
        public async Task<ServiceResult<List<RecipeResponse>>> GetRecipesByProjectPuid(string projectPuid)
        {
            // Get projectId from projectPuid
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<List<RecipeResponse>>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Redirect aliased project to canonical project PUID
            if (!string.IsNullOrWhiteSpace(project.Alias_Project_Puid))
            {
                return ServiceResult<List<RecipeResponse>>.Redirection(ServiceStatus.SeeOther303, $"/api/projects/{project.Alias_Project_Puid}/recipes");
            }

            // Get all recipes for the project
            var recipes = await _repo.GetByProjectId(project.Project_Id);
            var cachedProducts = new List<Product>();
            var recipeResponses = new List<RecipeResponse>();

            // Build responses using the fetched data
            foreach (var recipe in recipes)
            {
                var recipeProducts = await _recipeProductRepo.GetByRecipeId(recipe.Recipe_Id);

                // Fetch and cache products
                await CacheProducts(recipeProducts, cachedProducts);

                // Convert to API model
                var recipeResponse = ConvertToApiModel(recipe, recipeProducts, cachedProducts);
                recipeResponses.Add(recipeResponse);
            }
            return ServiceResult<List<RecipeResponse>>.SuccessResult(recipeResponses);
        }
        public async Task<ServiceResult> DeleteRecipe(string projectPuid, string puid)
        {
            // Get projectId from projectPuid
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Check if recipe exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
            var recipe = await _repo.GetByPuid(puid);
            if (recipe == null || recipe.Project_Id != project.Project_Id) return ServiceResult.Fail(ServiceStatus.NotFound404, "Recipe not found.");
            
            return await _repo.DeleteRecipe(recipe.Recipe_Id) 
                ? ServiceResult.SuccessResult(ServiceStatus.NoContent204) 
                : ServiceResult.Fail(ServiceStatus.InternalServerError500, "Failed to delete recipe.");
        }

        /// <summary>
        /// Handles the logic of determining which recipe products to add, update, and delete
        /// Converts incoming RecipeProductExchanges to RecipeProducts
        /// </summary>
        private (List<RecipeProduct> toAdd, List<RecipeProduct> toUpdate, List<RecipeProduct> toDelete) HandleRecipeProductUpdate(
            IEnumerable<RecipeProductExchange> inputs,
            IEnumerable<RecipeProductExchange> outputs,
            IEnumerable<RecipeProduct> existingRecipeProducts,
            int recipeId,
            IEnumerable<Product> cachedProducts)
        {
            var incomingRecipeProducts = new List<RecipeProduct>();
            incomingRecipeProducts.AddRange(AssembleRecipeProducts(inputs, recipeId, cachedProducts, isInput: true));
            incomingRecipeProducts.AddRange(AssembleRecipeProducts(outputs, recipeId, cachedProducts, isInput: false));

            return SeparateRecipeProductsForUpsert(incomingRecipeProducts, existingRecipeProducts);
        }

        /// <summary>
        /// Seperates incoming recipe products into those to add, update, and delete
        /// NOTE: toUpdate contains original RecipeProduct objects with updated quantities
        /// </summary>
        private (List<RecipeProduct> toAdd, List<RecipeProduct> toUpdate, List<RecipeProduct> toDelete)
            SeparateRecipeProductsForUpsert(
            IEnumerable<RecipeProduct> incomingRecipeProducts,
            IEnumerable<RecipeProduct> existingRecipeProducts)
        {
            var toAdd = new List<RecipeProduct>();
            var toUpdate = new List<RecipeProduct>();
            var toDelete = existingRecipeProducts
                .Where(er => !incomingRecipeProducts.Any(ir => ir.Product_Id == er.Product_Id && ir.Is_Input == er.Is_Input))
                .ToList();

            var existingProductIds = existingRecipeProducts
                .Select(rp => (rp.Product_Id, rp.Is_Input))
                .ToHashSet();

            foreach (var incomingRp in incomingRecipeProducts)
            {
                if (existingProductIds.Contains((incomingRp.Product_Id, incomingRp.Is_Input)))
                {
                    var existingRp = existingRecipeProducts
                        .First(er => er.Product_Id == incomingRp.Product_Id && er.Is_Input == incomingRp.Is_Input);
                    existingRp.Quantity = incomingRp.Quantity;
                    toUpdate.Add(existingRp);
                }
                else
                {
                    toAdd.Add(incomingRp);
                }
            }

            return (toAdd, toUpdate, toDelete);
        }

        /// <summary>
        /// Gets all products involved in the recipe products and caches them to avoid multiple DB calls
        /// Accepts list by reference
        /// </summary>
        private async Task CacheProducts(IEnumerable<RecipeProductExchange> recipeProducts, List<Product> cachedProducts)
        {
            foreach (var product in recipeProducts)
            {
                if (!cachedProducts.Any(p => p.Puid == product.Puid))
                {
                    var existingProduct = await _productRepo.GetProductByPuid(product.Puid);
                    if (existingProduct != null)
                    {
                        cachedProducts.Add(existingProduct);
                    }
                }
            }
        }
        /// <summary>
        /// Gets all products involved in the recipe products and caches them to avoid multiple DB calls
        /// Accepts list by reference
        /// </summary>
        private async Task CacheProducts(IEnumerable<RecipeProduct> recipeProducts, List<Product> cachedProducts)
        {
            foreach (var product in recipeProducts)
            {
                if (!cachedProducts.Any(p => p.Product_Id == product.Product_Id))
                {
                    var existingProduct = await _productRepo.GetProductById(product.Product_Id);
                    if (existingProduct != null)
                    {
                        cachedProducts.Add(existingProduct);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if the recipe_product relations are valid
        /// Criteria: 
        /// - products belong to the specified project
        /// - products exist
        /// - products are unique
        /// - quantity must be positive
        /// </summary>
        private (bool, string) CheckProducts(IEnumerable<RecipeProductExchange> products, int projectId, IEnumerable<Product> cachedProducts)
        {
            var productPuids = products.Select(i => i.Puid).ToList();
            // Check for duplicate product PUIDs
            var duplicatePuids = productPuids
                .GroupBy(x => x)
                .Where(g => g.Count() > 1) 
                .Select(g => g.Key) 
                .ToList();
            if (duplicatePuids.Any()) return (false, $"Duplicate product PUIDs found: {string.Join(", ", duplicatePuids)}");

            // Check if all products exist and belong to the project
            foreach (var product in products)
            {
                var cachedProduct = cachedProducts.FirstOrDefault(p => p.Puid == product.Puid);
                if (cachedProduct == null || cachedProduct.Project_Id != projectId)
                {
                    return (false, $"Product with PUID {product.Puid} is invalid.");
                }
            }

            // Check if all quantities are positive
            foreach (var product in products)
            {
                if (product.Quantity <= 0)
                {
                    return (false, $"Product with PUID {product.Puid} has non-positive quantity.");
                }
            }
            return (true, string.Empty);
        }

        private IEnumerable<RecipeProduct> AssembleRecipeProducts(IEnumerable<RecipeProductExchange> productExchanges, int recipeId, IEnumerable<Product> cachedProducts, bool isInput)
        {
            var recipeProducts = new List<RecipeProduct>();
            foreach (var exchange in productExchanges)
            {
                var product = cachedProducts.First(p => p.Puid == exchange.Puid);  // Must always exist due to prior validation
                var recipeProduct = new RecipeProduct
                {
                    Recipe_Product_Id = 0,
                    Recipe_Id = recipeId,
                    Product_Id = product.Product_Id,
                    Quantity = exchange.Quantity,
                    Is_Input = isInput
                };
                recipeProducts.Add(recipeProduct);
            }
            return recipeProducts;
        }

        private RecipeResponse ConvertToApiModel(Recipe recipe, IEnumerable<RecipeProduct> recipeProducts, IEnumerable<Product> cachedProducts)
        {
            var inputResponses = new List<RecipeProductExchange>();
            var outputResponses = new List<RecipeProductExchange>();
            foreach (var rp in recipeProducts)
            {
                var productPuid = cachedProducts.First(p => p.Product_Id == rp.Product_Id).Puid;  // Must always exist due to prior prior validation
                if (rp.Is_Input)
                {
                    inputResponses.Add(new RecipeProductExchange
                    {
                        Puid = productPuid,
                        Quantity = rp.Quantity
                    });
                }
                else
                {
                    outputResponses.Add(new RecipeProductExchange
                    {
                        Puid = productPuid,
                        Quantity = rp.Quantity
                    });
                }
            }

            var recipeResponse = new RecipeResponse
            {
                Puid = recipe.Puid,
                Name = recipe.Name,
                Description = recipe.Description,
                BaseCraftingTime = recipe.Base_Crafting_Time,
                Inputs = inputResponses,
                Outputs = outputResponses,
                CreatedAt = recipe.Created_At,
                UpdatedAt = recipe.Last_Updated
            };
            return recipeResponse;
        }
    }
}