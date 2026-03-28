using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Helpers;

namespace ProductionCalculator.Business.Services
{
    public class RecipeService : IRecipeService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IRecipeRepository _repo;
        private readonly IProductRepository _productRepo;
        private readonly IAttributeRepository _attributeRepo;
        private readonly IRecipeProductRepository _recipeProductRepo;
        private readonly IRecipeAttributeRepository _recipeAttributeRepo;
        private readonly IProjectRepository _projectRepo;
        public RecipeService(
            ICurrentUserService currentUser, 
            IProductRepository productRepo, 
            IAttributeRepository attributeRepo,
            IRecipeRepository repo, 
            IRecipeProductRepository recipeProductRepo, 
            IRecipeAttributeRepository recipeAttributeRepo,
            IProjectRepository projectRepo
            ) 
        { 
            _currentUser = currentUser; 
            _repo = repo;
            _productRepo = productRepo;
            _attributeRepo = attributeRepo;
            _recipeProductRepo = recipeProductRepo;
            _recipeAttributeRepo = recipeAttributeRepo;
            _projectRepo = projectRepo;
        }

        public async Task<ServiceResult<RecipeResponse>> AddRecipe(string projectPuid, string name, string? description, double baseCraftingTime, List<RecipeProductExchange> inputs, List<RecipeProductExchange> outputs, List<AttributeRateRequest>? attributes = null)
        {
            attributes ??= [];
            if (string.IsNullOrWhiteSpace(name)) return ServiceResult<RecipeResponse>.Fail(ServiceStatus.BadRequest400, "Recipe name is required.");

            // Get products
            var cachedProducts = new List<Product>();
            await CacheProducts(inputs.Concat(outputs).ToList(), cachedProducts); // Avoids multiple db calls

            // Get attributes
            var cachedAttributes = new List<ProjectAttribute>();
            await CacheAttributes(attributes, cachedAttributes);

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

            var (areAttributesValid, attributeError) = CheckAttributes(attributes, project.Project_Id, cachedAttributes);
            if (!areAttributesValid) return ServiceResult<RecipeResponse>.Fail(ServiceStatus.BadRequest400, attributeError);

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

            var recipeAttributes = AssembleRecipeAttributes(attributes, recipe.Recipe_Id, cachedAttributes);

            // Add recipe_products to DB
            await _recipeProductRepo.AddRecipeProducts(recipeProducts);
            await _recipeAttributeRepo.AddRecipeAttributes(recipeAttributes);
            await UpdateProjectLastUpdated(project);

            // Convert to API model
           var recipeResponse = ConvertToApiModel(recipe, recipeProducts, recipeAttributes, cachedProducts, cachedAttributes);
           return ServiceResult<RecipeResponse>.SuccessResult(recipeResponse, ServiceStatus.Created201);
        }
        public async Task<ServiceResult<RecipeResponse>> UpdateRecipe(string projectPuid, string puid, string name, string? description, double baseCraftingTime, List<RecipeProductExchange> inputs, List<RecipeProductExchange> outputs, List<AttributeRateRequest>? attributes = null)
        {
            attributes ??= [];
            if (string.IsNullOrWhiteSpace(name)) return ServiceResult<RecipeResponse>.Fail(ServiceStatus.BadRequest400, "Recipe name is required.");

            // Get products
            var cachedProducts = new List<Product>();
            await CacheProducts(inputs.Concat(outputs).ToList(), cachedProducts); // Avoids multiple db calls

            // Get attributes
            var cachedAttributes = new List<ProjectAttribute>();
            await CacheAttributes(attributes, cachedAttributes);

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

            var (areAttributesValid, attributeError) = CheckAttributes(attributes, project.Project_Id, cachedAttributes);
            if (!areAttributesValid) return ServiceResult<RecipeResponse>.Fail(ServiceStatus.BadRequest400, attributeError);

            // Check baseCraftingTime is positive
            if (baseCraftingTime <= 0) return ServiceResult<RecipeResponse>.Fail(ServiceStatus.BadRequest400, "Base crafting time must be positive.");

            // Limit string lengths
            name = TruncateHelper.TruncateString(name, 255);
            description = TruncateHelper.TruncateStringNullable(description, 1000);

            // Handle recipe products upsert
            var existingRecipeProducts = await _recipeProductRepo.GetByRecipeId(recipe.Recipe_Id);
            var (toAdd, toUpdate, toDelete) = HandleRecipeProductUpdate(inputs, outputs, existingRecipeProducts, recipe.Recipe_Id, cachedProducts);

            var existingRecipeAttributes = await _recipeAttributeRepo.GetByRecipeId(recipe.Recipe_Id);
            var (attributesToAdd, attributesToUpdate, attributesToDelete) = HandleRecipeAttributeUpdate(attributes, existingRecipeAttributes, recipe.Recipe_Id, cachedAttributes);

            await _recipeProductRepo.AddRecipeProducts(toAdd);
            await _recipeProductRepo.UpdateRecipeProducts(toUpdate);
            await _recipeProductRepo.DeleteRecipeProducts(toDelete.Select(rp => rp.Recipe_Product_Id));

            await _recipeAttributeRepo.AddRecipeAttributes(attributesToAdd);
            await _recipeAttributeRepo.UpdateRecipeAttributes(attributesToUpdate);
            await _recipeAttributeRepo.DeleteRecipeAttributes(attributesToDelete.Select(ra => ra.Recipe_Attribute_Id));

            // Update recipe in DB
            recipe.Name = name;
            recipe.Description = description ?? string.Empty;
            if (recipe.Base_Crafting_Time != baseCraftingTime || toAdd.Any() || toUpdate.Any() || toDelete.Any() || attributesToAdd.Any() || attributesToUpdate.Any() || attributesToDelete.Any()) recipe.Version += 1; // Only increment version if tangible change
            recipe.Base_Crafting_Time = baseCraftingTime;
            recipe.Last_Updated = DateTime.UtcNow;
            await _repo.UpdateRecipe(recipe);

            await UpdateProjectLastUpdated(project);

            // Convert to API model
            var finalRecipeProducts = (await _recipeProductRepo.GetByRecipeId(recipe.Recipe_Id)).ToList();
            var finalRecipeAttributes = (await _recipeAttributeRepo.GetByRecipeId(recipe.Recipe_Id)).ToList();
            await CacheProducts(finalRecipeProducts, cachedProducts);
            await CacheAttributes(finalRecipeAttributes, cachedAttributes);
            var recipeResponse = ConvertToApiModel(recipe, finalRecipeProducts, finalRecipeAttributes, cachedProducts, cachedAttributes);
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
                return ServiceResult<RecipeResponse>.Redirection(ServiceStatus.SeeOther303, $"/projects/{project.Alias_Project_Puid}/recipes/{puid}");
            }

            // Check if recipe exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
            var recipe = await _repo.GetByPuid(puid);
            if (recipe == null || recipe.Project_Id != project.Project_Id) return ServiceResult<RecipeResponse>.Fail(ServiceStatus.NotFound404, "Recipe not found.");

            // Get recipe products
            var recipeProducts = await _recipeProductRepo.GetByRecipeId(recipe.Recipe_Id);
            var recipeAttributes = await _recipeAttributeRepo.GetByRecipeId(recipe.Recipe_Id);

            // Cache products
            var cachedProducts = new List<Product>();
            await CacheProducts(recipeProducts, cachedProducts);

            // Cache attributes
            var cachedAttributes = new List<ProjectAttribute>();
            await CacheAttributes(recipeAttributes, cachedAttributes);
            
            // Convert to API model
            var recipeResponse = ConvertToApiModel(recipe, recipeProducts, recipeAttributes, cachedProducts, cachedAttributes);
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
                return ServiceResult<List<RecipeResponse>>.Redirection(ServiceStatus.SeeOther303, $"/projects/{project.Alias_Project_Puid}/recipes");
            }

            // Get all recipes for the project
            var recipes = await _repo.GetByProjectId(project.Project_Id);
            var cachedProducts = new List<Product>();
            var recipeResponses = new List<RecipeResponse>();

            // Build responses using the fetched data
            foreach (var recipe in recipes)
            {
                var recipeProducts = await _recipeProductRepo.GetByRecipeId(recipe.Recipe_Id);
                var recipeAttributes = await _recipeAttributeRepo.GetByRecipeId(recipe.Recipe_Id);

                // Fetch and cache products
                await CacheProducts(recipeProducts, cachedProducts);

                var cachedAttributes = new List<ProjectAttribute>();
                await CacheAttributes(recipeAttributes, cachedAttributes);

                // Convert to API model
                var recipeResponse = ConvertToApiModel(recipe, recipeProducts, recipeAttributes, cachedProducts, cachedAttributes);
                recipeResponses.Add(recipeResponse);
            }
            return ServiceResult<List<RecipeResponse>>.SuccessResult(recipeResponses);
        }

        private (List<RecipeAttribute> toAdd, List<RecipeAttribute> toUpdate, List<RecipeAttribute> toDelete) HandleRecipeAttributeUpdate(
            IEnumerable<AttributeRateRequest> attributes,
            IEnumerable<RecipeAttribute> existingRecipeAttributes,
            int recipeId,
            IEnumerable<ProjectAttribute> cachedAttributes)
        {
            var incomingRecipeAttributes = AssembleRecipeAttributes(attributes, recipeId, cachedAttributes);
            return SeparateRecipeAttributesForUpsert(incomingRecipeAttributes, existingRecipeAttributes);
        }

        private (List<RecipeAttribute> toAdd, List<RecipeAttribute> toUpdate, List<RecipeAttribute> toDelete)
            SeparateRecipeAttributesForUpsert(
            IEnumerable<RecipeAttribute> incomingRecipeAttributes,
            IEnumerable<RecipeAttribute> existingRecipeAttributes)
        {
            var toAdd = new List<RecipeAttribute>();
            var toUpdate = new List<RecipeAttribute>();
            var toDelete = existingRecipeAttributes
                .Where(er => !incomingRecipeAttributes.Any(ir => ir.Attribute_Id == er.Attribute_Id))
                .ToList();

            var existingAttributeIds = existingRecipeAttributes
                .Select(ra => ra.Attribute_Id)
                .ToHashSet();

            foreach (var incomingRa in incomingRecipeAttributes)
            {
                if (existingAttributeIds.Contains(incomingRa.Attribute_Id))
                {
                    var existingRa = existingRecipeAttributes.First(er => er.Attribute_Id == incomingRa.Attribute_Id);
                    existingRa.Rate = incomingRa.Rate;
                    existingRa.Last_Updated = DateTime.UtcNow;
                    existingRa.Version += 1;
                    toUpdate.Add(existingRa);
                }
                else
                {
                    toAdd.Add(incomingRa);
                }
            }

            return (toAdd, toUpdate, toDelete);
        }
        public async Task<ServiceResult> DeleteRecipe(string projectPuid, string puid)
        {
            // Get projectId from projectPuid
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Check if recipe exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
            var recipe = await _repo.GetByPuid(puid);
            if (recipe == null || recipe.Project_Id != project.Project_Id) return ServiceResult.Fail(ServiceStatus.NotFound404, "Recipe not found.");
            
            var isDeleted = await _repo.DeleteRecipe(recipe.Recipe_Id);
            if (!isDeleted) return ServiceResult.Fail(ServiceStatus.InternalServerError500, "Failed to delete recipe.");

            await UpdateProjectLastUpdated(project);
            return ServiceResult.SuccessResult(ServiceStatus.NoContent204);
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

        private async Task CacheAttributes(IEnumerable<AttributeRateRequest> attributes, List<ProjectAttribute> cachedAttributes)
        {
            foreach (var attribute in attributes)
            {
                if (!cachedAttributes.Any(a => a.Puid == attribute.Puid))
                {
                    var existingAttribute = await _attributeRepo.GetAttributeByPuid(attribute.Puid);
                    if (existingAttribute != null)
                    {
                        cachedAttributes.Add(existingAttribute);
                    }
                }
            }
        }

        private async Task CacheAttributes(IEnumerable<RecipeAttribute> recipeAttributes, List<ProjectAttribute> cachedAttributes)
        {
            foreach (var attribute in recipeAttributes)
            {
                if (!cachedAttributes.Any(a => a.Attribute_Id == attribute.Attribute_Id))
                {
                    var existingAttribute = await _attributeRepo.GetAttributeById(attribute.Attribute_Id);
                    if (existingAttribute != null)
                    {
                        cachedAttributes.Add(existingAttribute);
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

        private (bool, string) CheckAttributes(IEnumerable<AttributeRateRequest> attributes, int projectId, IEnumerable<ProjectAttribute> cachedAttributes)
        {
            var attributePuids = attributes.Select(i => i.Puid).ToList();
            var duplicatePuids = attributePuids
                .GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicatePuids.Any()) return (false, $"Duplicate attribute PUIDs found: {string.Join(", ", duplicatePuids)}");

            foreach (var attribute in attributes)
            {
                var cachedAttribute = cachedAttributes.FirstOrDefault(a => a.Puid == attribute.Puid);
                if (cachedAttribute == null || cachedAttribute.Project_Id != projectId)
                {
                    return (false, $"Attribute with PUID {attribute.Puid} is invalid.");
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

        private List<RecipeAttribute> AssembleRecipeAttributes(IEnumerable<AttributeRateRequest> attributeExchanges, int recipeId, IEnumerable<ProjectAttribute> cachedAttributes)
        {
            var recipeAttributes = new List<RecipeAttribute>();
            foreach (var exchange in attributeExchanges)
            {
                var attribute = cachedAttributes.First(a => a.Puid == exchange.Puid);
                var recipeAttribute = new RecipeAttribute
                {
                    Recipe_Attribute_Id = 0,
                    Recipe_Id = recipeId,
                    Attribute_Id = attribute.Attribute_Id,
                    Rate = exchange.Rate,
                    Version = 1,
                    Created_At = DateTime.UtcNow,
                    Last_Updated = DateTime.UtcNow
                };
                recipeAttributes.Add(recipeAttribute);
            }

            return recipeAttributes;
        }

        private RecipeResponse ConvertToApiModel(Recipe recipe, IEnumerable<RecipeProduct> recipeProducts, IEnumerable<RecipeAttribute> recipeAttributes, IEnumerable<Product> cachedProducts, IEnumerable<ProjectAttribute> cachedAttributes)
        {
            var inputResponses = new List<RecipeProductExchange>();
            var outputResponses = new List<RecipeProductExchange>();
            var attributeResponses = new List<AttributeRateResponse>();
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

            foreach (var recipeAttribute in recipeAttributes)
            {
                var attributePuid = cachedAttributes.First(a => a.Attribute_Id == recipeAttribute.Attribute_Id).Puid;
                attributeResponses.Add(new AttributeRateResponse
                {
                    Puid = attributePuid,
                    Rate = recipeAttribute.Rate
                });
            }

            var recipeResponse = new RecipeResponse
            {
                Puid = recipe.Puid,
                Name = recipe.Name,
                Description = recipe.Description,
                BaseCraftingTime = recipe.Base_Crafting_Time,
                Inputs = inputResponses,
                Outputs = outputResponses,
                Attributes = attributeResponses,
                CreatedAt = recipe.Created_At,
                UpdatedAt = recipe.Last_Updated
            };
            return recipeResponse;
        }
        private async Task UpdateProjectLastUpdated(Project project)
        {
            project.Last_Updated = DateTime.UtcNow;
            await _projectRepo.UpdateProject(project);
        }
    }
}