using FakeItEasy;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Services;
using ProductionCalculator.Business.APIModels;

namespace ProductionCalculator.Business.Tests;

public class RecipeServiceTests
{
    private static Project CreateProject(int id = 1, string puid = "project123", string? aliasPuid = null)
    {
        return new Project
        {
            Project_Id = id,
            User_Id = 1,
            Puid = puid,
            Name = "Test Project",
            Alias_Project_Puid = aliasPuid,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    private static Recipe CreateRecipe(int id = 1, int projectId = 1, string puid = "recipe123", string name = "Recipe")
    {
        return new Recipe
        {
            Recipe_Id = id,
            Project_Id = projectId,
            Puid = puid,
            Name = name,
            Description = "Description",
            Base_Crafting_Time = 1.0,
            Version = 1,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    private static Product CreateProduct(int id = 1, int projectId = 1, string puid = "prodPuid", string name = "Product")
    {
        return new Product
        {
            Product_Id = id,
            Project_Id = projectId,
            Puid = puid,
            Name = name,
            Description = "desc",
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    private static RecipeService CreateService(
        IRecipeRepository repo,
        IProductRepository productRepo,
        IAttributeRepository attributeRepo,
        IRecipeProductRepository recipeProductRepo,
        IRecipeAttributeRepository recipeAttributeRepo,
        IProjectRepository projectRepo)
    {
        var currentUser = A.Fake<ICurrentUserService>();
        return new RecipeService(currentUser, productRepo, attributeRepo, repo, recipeProductRepo, recipeAttributeRepo, projectRepo);
    }

    [Fact]
    public async Task AddRecipe_EmptyName_ReturnsBadRequest()
    {
        var repo = A.Fake<IRecipeRepository>();
        var productRepo = A.Fake<IProductRepository>();
        var recipeProductRepo = A.Fake<IRecipeProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, productRepo, A.Fake<IAttributeRepository>(), recipeProductRepo, A.Fake<IRecipeAttributeRepository>(), projectRepo);

        var result = await service.AddRecipe("projPuid", "", "desc", 1.0, new(), new());

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task AddRecipe_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IRecipeRepository>();
        var productRepo = A.Fake<IProductRepository>();
        var recipeProductRepo = A.Fake<IRecipeProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, productRepo, A.Fake<IAttributeRepository>(), recipeProductRepo, A.Fake<IRecipeAttributeRepository>(), projectRepo);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.AddRecipe("missing", "Recipe", "desc", 1.0, new(), new());

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task AddRecipe_DuplicateNameInProject_ReturnsConflict()
    {
        var repo = A.Fake<IRecipeRepository>();
        var productRepo = A.Fake<IProductRepository>();
        var recipeProductRepo = A.Fake<IRecipeProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, productRepo, A.Fake<IAttributeRepository>(), recipeProductRepo, A.Fake<IRecipeAttributeRepository>(), projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetByProjectId(10)).Returns(new List<Recipe> { CreateRecipe(name: "Existing") });

        var result = await service.AddRecipe("projPuid", "Existing", "desc", 1.0, new(), new());

        Assert.Equal(ServiceStatus.Conflict409, result.Status);
    }

    [Fact]
    public async Task AddRecipe_ValidRequest_ReturnsCreatedAndSavesToRepo()
    {
        var repo = A.Fake<IRecipeRepository>();
        var productRepo = A.Fake<IProductRepository>();
        var recipeProductRepo = A.Fake<IRecipeProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var attributeRepo = A.Fake<IAttributeRepository>();
        var recipeAttributeRepo = A.Fake<IRecipeAttributeRepository>();
        var service = CreateService(repo, productRepo, attributeRepo, recipeProductRepo, recipeAttributeRepo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        var product = CreateProduct(id: 5, projectId: 10, puid: "p1");
        var attribute = new ProjectAttribute
        {
            Attribute_Id = 7,
            Project_Id = 10,
            Puid = "a1",
            Name = "Attr",
            Description = "desc",
            Unit = "u",
            Version = 1,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };

        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetByProjectId(10)).Returns(new List<Recipe>());
        A.CallTo(() => productRepo.GetProductByPuid("p1")).Returns(product);
        A.CallTo(() => attributeRepo.GetAttributeByPuid("a1")).Returns(attribute);
        A.CallTo(() => repo.PuidExists(A<string>._)).Returns(false);

        var inputs = new List<RecipeProductExchange> { new() { Puid = "p1", Quantity = 5 } };
        var outputs = new List<RecipeProductExchange>();
        var attributes = new List<AttributeRateExchange> { new() { Puid = "a1", Rate = 2 } };

        var result = await service.AddRecipe("projPuid", "NewRecipe", "desc", 2.0, inputs, outputs, attributes);

        Assert.True(result.Success);
        Assert.Equal(ServiceStatus.Created201, result.Status);
        A.CallTo(() => repo.AddRecipe(A<Recipe>.That.Matches(r => r.Name == "NewRecipe" && r.Project_Id == 10))).MustHaveHappenedOnceExactly();
        A.CallTo(() => recipeProductRepo.AddRecipeProducts(A<IEnumerable<RecipeProduct>>.That.Matches(rp => rp.Any(x => x.Product_Id == 5 && x.Is_Input)))).MustHaveHappenedOnceExactly();
        A.CallTo(() => recipeAttributeRepo.AddRecipeAttributes(A<IEnumerable<RecipeAttribute>>.That.Matches(ra => ra.Any(x => x.Attribute_Id == 7 && x.Rate == 2)))).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetRecipeByPuid_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IRecipeRepository>();
        var productRepo = A.Fake<IProductRepository>();
        var recipeProductRepo = A.Fake<IRecipeProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, productRepo, A.Fake<IAttributeRepository>(), recipeProductRepo, A.Fake<IRecipeAttributeRepository>(), projectRepo);
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(Task.FromResult<Project?>(null));

        var result = await service.GetRecipeByPuid("projPuid", "recipePuid");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetRecipeByPuid_RecipeNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IRecipeRepository>();
        var productRepo = A.Fake<IProductRepository>();
        var recipeProductRepo = A.Fake<IRecipeProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, productRepo, A.Fake<IAttributeRepository>(), recipeProductRepo, A.Fake<IRecipeAttributeRepository>(), projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetByPuid("missing")).Returns(Task.FromResult<Recipe?>(null));

        var result = await service.GetRecipeByPuid("projPuid", "missing");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetRecipeByPuid_ValidInputs_ReturnsRecipe()
    {
        var repo = A.Fake<IRecipeRepository>();
        var productRepo = A.Fake<IProductRepository>();
        var recipeProductRepo = A.Fake<IRecipeProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, productRepo, A.Fake<IAttributeRepository>(), recipeProductRepo, A.Fake<IRecipeAttributeRepository>(), projectRepo);
        var project = CreateProject(id: 10);
        var recipe = CreateRecipe(id: 1, projectId: 10, puid: "r1");
        var product = CreateProduct(id: 5, puid: "p1");
        var rp = new RecipeProduct { Recipe_Product_Id = 1, Recipe_Id = 1, Product_Id = 5, Quantity = 10, Is_Input = true };

        A.CallTo(() => projectRepo.GetProjectByPuid("project123")).Returns(project);
        A.CallTo(() => repo.GetByPuid("r1")).Returns(recipe);
        A.CallTo(() => recipeProductRepo.GetByRecipeId(1)).Returns(new List<RecipeProduct> { rp });
        A.CallTo(() => productRepo.GetProductById(5)).Returns(product);

        var result = await service.GetRecipeByPuid("project123", "r1");

        Assert.True(result.Success);
        Assert.Equal("r1", result.Data!.Puid);
        Assert.Single(result.Data.Inputs);
        Assert.Equal("p1", result.Data.Inputs[0].Puid);
    }

    [Fact]
    public async Task GetRecipesByProjectPuid_ProjectNotFound_ReturnsNotFound()
    {
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(A.Fake<IRecipeRepository>(), A.Fake<IProductRepository>(), A.Fake<IAttributeRepository>(), A.Fake<IRecipeProductRepository>(), A.Fake<IRecipeAttributeRepository>(), projectRepo);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.GetRecipesByProjectPuid("missing");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetRecipesByProjectPuid_ProjectExists_ReturnsRecipeList()
    {
        var repo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, A.Fake<IProductRepository>(), A.Fake<IAttributeRepository>(), A.Fake<IRecipeProductRepository>(), A.Fake<IRecipeAttributeRepository>(), projectRepo);
        var project = CreateProject(id: 10);
        A.CallTo(() => projectRepo.GetProjectByPuid("project123")).Returns(project);
        A.CallTo(() => repo.GetByProjectId(10)).Returns(new List<Recipe> { CreateRecipe(puid: "r1"), CreateRecipe(puid: "r2") });

        var result = await service.GetRecipesByProjectPuid("project123");

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Count);
    }

    [Fact]
    public async Task UpdateRecipe_EmptyName_ReturnsBadRequest()
    {
        var service = CreateService(A.Fake<IRecipeRepository>(), A.Fake<IProductRepository>(), A.Fake<IAttributeRepository>(), A.Fake<IRecipeProductRepository>(), A.Fake<IRecipeAttributeRepository>(), A.Fake<IProjectRepository>());

        var result = await service.UpdateRecipe("proj", "recipe", "", "desc", 1.0, new(), new());

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task UpdateRecipe_ProjectNotFound_ReturnsNotFound()
    {
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(A.Fake<IRecipeRepository>(), A.Fake<IProductRepository>(), A.Fake<IAttributeRepository>(), A.Fake<IRecipeProductRepository>(), A.Fake<IRecipeAttributeRepository>(), projectRepo);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.UpdateRecipe("missing", "recipe", "Name", "desc", 1.0, new(), new());

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateRecipe_RecipeNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, A.Fake<IProductRepository>(), A.Fake<IAttributeRepository>(), A.Fake<IRecipeProductRepository>(), A.Fake<IRecipeAttributeRepository>(), projectRepo);
        var project = CreateProject(id: 10);
        A.CallTo(() => projectRepo.GetProjectByPuid("proj")).Returns(project);
        A.CallTo(() => repo.GetByPuid("missing")).Returns(Task.FromResult<Recipe?>(null));

        var result = await service.UpdateRecipe("proj", "missing", "Name", "desc", 1.0, new(), new());

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateRecipe_DuplicateNameOtherThanSelf_ReturnsConflict()
    {
        var repo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, A.Fake<IProductRepository>(), A.Fake<IAttributeRepository>(), A.Fake<IRecipeProductRepository>(), A.Fake<IRecipeAttributeRepository>(), projectRepo);
        var project = CreateProject(id: 10);
        var recipe = CreateRecipe(id: 1, projectId: 10, puid: "r1");
        A.CallTo(() => projectRepo.GetProjectByPuid("proj")).Returns(project);
        A.CallTo(() => repo.GetByPuid("r1")).Returns(recipe);
        A.CallTo(() => repo.GetByProjectId(10)).Returns(new List<Recipe> { recipe, CreateRecipe(id: 2, name: "Other") });

        var result = await service.UpdateRecipe("proj", "r1", "Other", "desc", 1.0, new(), new());

        Assert.Equal(ServiceStatus.Conflict409, result.Status);
    }

    [Fact]
    public async Task UpdateRecipe_ValidRequest_ReturnsSuccessAndUpdatesRepo()
    {
        var repo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var recipeProductRepo = A.Fake<IRecipeProductRepository>();
        var service = CreateService(repo, A.Fake<IProductRepository>(), A.Fake<IAttributeRepository>(), recipeProductRepo, A.Fake<IRecipeAttributeRepository>(), projectRepo);
        var project = CreateProject(id: 10);
        var recipe = CreateRecipe(id: 1, projectId: 10, puid: "r1");

        A.CallTo(() => projectRepo.GetProjectByPuid("proj")).Returns(project);
        A.CallTo(() => repo.GetByPuid("r1")).Returns(recipe);
        A.CallTo(() => repo.GetByProjectId(10)).Returns(new List<Recipe> { recipe });
        A.CallTo(() => recipeProductRepo.GetByRecipeId(1)).Returns(new List<RecipeProduct>());

        var result = await service.UpdateRecipe("proj", "r1", "Updated", "desc", 5.0, new(), new());

        Assert.True(result.Success);
        A.CallTo(() => repo.UpdateRecipe(A<Recipe>.That.Matches(r => r.Name == "Updated" && r.Base_Crafting_Time == 5.0))).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteRecipe_ProjectNotFound_ReturnsNotFound()
    {
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(A.Fake<IRecipeRepository>(), A.Fake<IProductRepository>(), A.Fake<IAttributeRepository>(), A.Fake<IRecipeProductRepository>(), A.Fake<IRecipeAttributeRepository>(), projectRepo);
        A.CallTo(() => projectRepo.GetProjectByPuid("proj")).Returns(Task.FromResult<Project?>(null));

        var result = await service.DeleteRecipe("proj", "recipe");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task DeleteRecipe_RecipeNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, A.Fake<IProductRepository>(), A.Fake<IAttributeRepository>(), A.Fake<IRecipeProductRepository>(), A.Fake<IRecipeAttributeRepository>(), projectRepo);
        var project = CreateProject(id: 10);
        A.CallTo(() => projectRepo.GetProjectByPuid("proj")).Returns(project);
        A.CallTo(() => repo.GetByPuid("missing")).Returns(Task.FromResult<Recipe?>(null));

        var result = await service.DeleteRecipe("proj", "missing");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task DeleteRecipe_RepoReturnsFalse_ReturnsInternalServerError()
    {
        var repo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, A.Fake<IProductRepository>(), A.Fake<IAttributeRepository>(), A.Fake<IRecipeProductRepository>(), A.Fake<IRecipeAttributeRepository>(), projectRepo);
        var project = CreateProject(id: 10);
        var recipe = CreateRecipe(id: 1, projectId: 10, puid: "r1");
        A.CallTo(() => projectRepo.GetProjectByPuid("proj")).Returns(project);
        A.CallTo(() => repo.GetByPuid("r1")).Returns(recipe);
        A.CallTo(() => repo.DeleteRecipe(1)).Returns(false);

        var result = await service.DeleteRecipe("proj", "r1");

        Assert.Equal(ServiceStatus.InternalServerError500, result.Status);
    }

    [Fact]
    public async Task DeleteRecipe_ValidRequest_ReturnsNoContent()
    {
        var repo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, A.Fake<IProductRepository>(), A.Fake<IAttributeRepository>(), A.Fake<IRecipeProductRepository>(), A.Fake<IRecipeAttributeRepository>(), projectRepo);
        var project = CreateProject(id: 10);
        var recipe = CreateRecipe(id: 1, projectId: 10, puid: "r1");
        A.CallTo(() => projectRepo.GetProjectByPuid("proj")).Returns(project);
        A.CallTo(() => repo.GetByPuid("r1")).Returns(recipe);
        A.CallTo(() => repo.DeleteRecipe(1)).Returns(true);

        var result = await service.DeleteRecipe("proj", "r1");

        Assert.Equal(ServiceStatus.NoContent204, result.Status);
        A.CallTo(() => projectRepo.UpdateProject(A<Project>._)).MustHaveHappenedOnceExactly();
    }
}
