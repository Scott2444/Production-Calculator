using System.Diagnostics.CodeAnalysis;
using FakeItEasy;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Services;

namespace ProductionCalculator.Business.Tests;

[ExcludeFromCodeCoverage]
public class ProductServiceTests
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

    private static Product CreateProduct(int id = 1, int projectId = 1, string puid = "product123", string name = "Product")
    {
        return new Product
        {
            Product_Id = id,
            Project_Id = projectId,
            Puid = puid,
            Name = name,
            Description = "Description",
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    private static ProductService CreateService(IProductRepository repo, IProjectRepository projectRepo)
    {
        var currentUser = A.Fake<ICurrentUserService>();
        return new ProductService(currentUser, repo, projectRepo);
    }

    [Fact]
    public async Task AddProduct_EmptyName_ReturnsBadRequest()
    {
        var repo = A.Fake<IProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);

        var result = await service.AddProduct("projectPuid", "", "desc");

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task AddProduct_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.AddProduct("missing", "Prod", "desc");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task AddProduct_DuplicateNameInProject_ReturnsConflict()
    {
        var repo = A.Fake<IProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetProductsByProjectId(10)).Returns(new List<Product> { CreateProduct(name: "Existing") });

        var result = await service.AddProduct("projPuid", "Existing", "desc");

        Assert.Equal(ServiceStatus.Conflict409, result.Status);
    }

    [Fact]
    public async Task AddProduct_ValidRequest_ReturnsCreatedAndSavesToRepo()
    {
        var repo = A.Fake<IProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetProductsByProjectId(10)).Returns(new List<Product>());
        A.CallTo(() => repo.PuidExists(A<string>._)).Returns(false);

        var result = await service.AddProduct("projPuid", "NewProd", "desc");

        Assert.True(result.Success);
        Assert.Equal(ServiceStatus.Created201, result.Status);
        A.CallTo(() => repo.AddProduct(A<Product>.That.Matches(p => p.Name == "NewProd" && p.Project_Id == 10))).MustHaveHappenedOnceExactly();
        A.CallTo(() => projectRepo.UpdateProject(A<Project>.That.Matches(p => p.Project_Id == 10))).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetProductByPuid_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.GetProductByPuid("missing", "prodPuid");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetProductByPuid_AliasedProject_RedirectsToCanonical()
    {
        var repo = A.Fake<IProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(puid: "alias", aliasPuid: "canonical");
        A.CallTo(() => projectRepo.GetProjectByPuid("alias")).Returns(project);

        var result = await service.GetProductByPuid("alias", "prodPuid");

        Assert.Equal(ServiceStatus.SeeOther303, result.Status);
        Assert.Equal("/api/projects/canonical/products/prodPuid", result.Location);
    }

    [Fact]
    public async Task GetProductByPuid_ProductNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 1, puid: "proj");
        A.CallTo(() => projectRepo.GetProjectByPuid("proj")).Returns(project);
        A.CallTo(() => repo.GetProductByPuid("missing")).Returns(Task.FromResult<Product?>(null));

        var result = await service.GetProductByPuid("proj", "missing");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetProductByPuid_ProductBelongsToDifferentProject_ReturnsNotFound()
    {
        var repo = A.Fake<IProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 1, puid: "proj");
        var otherProduct = CreateProduct(id: 10, projectId: 2, puid: "prodPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("proj")).Returns(project);
        A.CallTo(() => repo.GetProductByPuid("prodPuid")).Returns(otherProduct);

        var result = await service.GetProductByPuid("proj", "prodPuid");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetProductByPuid_ValidInputs_ReturnsProduct()
    {
        var repo = A.Fake<IProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 1, puid: "proj");
        var product = CreateProduct(id: 10, projectId: 1, puid: "prodPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("proj")).Returns(project);
        A.CallTo(() => repo.GetProductByPuid("prodPuid")).Returns(product);

        var result = await service.GetProductByPuid("proj", "prodPuid");

        Assert.True(result.Success);
        Assert.Equal(product, result.Data);
    }

    [Fact]
    public async Task GetProductsByProjectPuid_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.GetProductsByProjectPuid("missing");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetProductsByProjectPuid_AliasedProject_RedirectsToCanonical()
    {
        var repo = A.Fake<IProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(puid: "alias", aliasPuid: "canonical");
        A.CallTo(() => projectRepo.GetProjectByPuid("alias")).Returns(project);

        var result = await service.GetProductsByProjectPuid("alias");

        Assert.Equal(ServiceStatus.SeeOther303, result.Status);
        Assert.Equal("/api/projects/canonical/products", result.Location);
    }

    [Fact]
    public async Task GetProductsByProjectPuid_ProjectExists_ReturnsProductList()
    {
        var repo = A.Fake<IProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 1, puid: "proj");
        var products = new List<Product> { CreateProduct(id: 1, projectId: 1), CreateProduct(id: 2, projectId: 1) };
        A.CallTo(() => projectRepo.GetProjectByPuid("proj")).Returns(project);
        A.CallTo(() => repo.GetProductsByProjectId(1)).Returns(products);

        var result = await service.GetProductsByProjectPuid("proj");

        Assert.True(result.Success);
        Assert.Equal(products, result.Data);
    }

    [Fact]
    public async Task UpdateProduct_EmptyName_ReturnsBadRequest()
    {
        var repo = A.Fake<IProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);

        var result = await service.UpdateProduct("proj", "prod", "", "desc");

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task UpdateProduct_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.UpdateProduct("missing", "prod", "Name", "desc");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateProduct_ProductNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 1, puid: "proj");
        A.CallTo(() => projectRepo.GetProjectByPuid("proj")).Returns(project);
        A.CallTo(() => repo.GetProductByPuid("missing")).Returns(Task.FromResult<Product?>(null));

        var result = await service.UpdateProduct("proj", "missing", "Name", "desc");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateProduct_ProductBelongsToDifferentProject_ReturnsNotFound()
    {
        var repo = A.Fake<IProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 1, puid: "proj");
        var otherProduct = CreateProduct(id: 10, projectId: 2, puid: "prodPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("proj")).Returns(project);
        A.CallTo(() => repo.GetProductByPuid("prodPuid")).Returns(otherProduct);

        var result = await service.UpdateProduct("proj", "prodPuid", "Name", "desc");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateProduct_DuplicateNameOtherThanSelf_ReturnsConflict()
    {
        var repo = A.Fake<IProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id:1, puid: "proj");
        var product = CreateProduct(id: 10, projectId: 1, puid: "prodPuid", name: "Original");
        var otherProduct = CreateProduct(id: 11, projectId: 1, puid: "other", name: "Dupe");
        
        A.CallTo(() => projectRepo.GetProjectByPuid("proj")).Returns(project);
        A.CallTo(() => repo.GetProductByPuid("prodPuid")).Returns(product);
        A.CallTo(() => repo.GetProductsByProjectId(1)).Returns(new List<Product> { product, otherProduct });

        var result = await service.UpdateProduct("proj", "prodPuid", "Dupe", "desc");

        Assert.Equal(ServiceStatus.Conflict409, result.Status);
    }

    [Fact]
    public async Task UpdateProduct_ValidRequest_ReturnsSuccessAndUpdatesRepo()
    {
        var repo = A.Fake<IProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 1, puid: "proj");
        var product = CreateProduct(id: 10, projectId: 1, puid: "prodPuid", name: "Original");
        A.CallTo(() => projectRepo.GetProjectByPuid("proj")).Returns(project);
        A.CallTo(() => repo.GetProductByPuid("prodPuid")).Returns(product);
        A.CallTo(() => repo.GetProductsByProjectId(1)).Returns(new List<Product> { product });

        var result = await service.UpdateProduct("proj", "prodPuid", "NewName", "NewDesc");

        Assert.True(result.Success);
        Assert.Equal("NewName", product.Name);
        Assert.Equal("NewDesc", product.Description);
        A.CallTo(() => repo.UpdateProduct(product)).MustHaveHappenedOnceExactly();
        A.CallTo(() => projectRepo.UpdateProject(project)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteProduct_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.DeleteProduct("missing", "prod");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task DeleteProduct_ProductNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 1, puid: "proj");
        A.CallTo(() => projectRepo.GetProjectByPuid("proj")).Returns(project);
        A.CallTo(() => repo.GetProductByPuid("missing")).Returns(Task.FromResult<Product?>(null));

        var result = await service.DeleteProduct("proj", "missing");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task DeleteProduct_RepoReturnsFalse_ReturnsInternalServerError()
    {
        var repo = A.Fake<IProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 1, puid: "proj");
        var product = CreateProduct(id: 10, projectId: 1, puid: "prodPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("proj")).Returns(project);
        A.CallTo(() => repo.GetProductByPuid("prodPuid")).Returns(product);
        A.CallTo(() => repo.DeleteProduct(10)).Returns(false);

        var result = await service.DeleteProduct("proj", "prodPuid");

        Assert.Equal(ServiceStatus.InternalServerError500, result.Status);
    }

    [Fact]
    public async Task DeleteProduct_ValidRequest_ReturnsNoContent()
    {
        var repo = A.Fake<IProductRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 1, puid: "proj");
        var product = CreateProduct(id: 10, projectId: 1, puid: "prodPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("proj")).Returns(project);
        A.CallTo(() => repo.GetProductByPuid("prodPuid")).Returns(product);
        A.CallTo(() => repo.DeleteProduct(10)).Returns(true);

        var result = await service.DeleteProduct("proj", "prodPuid");

        Assert.True(result.Success);
        Assert.Equal(ServiceStatus.NoContent204, result.Status);
        A.CallTo(() => projectRepo.UpdateProject(project)).MustHaveHappenedOnceExactly();
    }
}
