using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

public class ProductRepositoryTests
{
    private static ProductionCalculatorDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ProductionCalculatorDbContext>()
            .UseInMemoryDatabase(databaseName: $"pc-tests-{Guid.NewGuid()}")
            .Options;

        var db = new ProductionCalculatorDbContext(options);
        db.Database.EnsureCreated();
        return db;
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

    [Fact]
    public async Task AddProduct_ValidProduct_AddsProductToDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new ProductRepository(db);
        var product = CreateProduct(id: 0); // EF will assign id

        await repo.AddProduct(product);

        var saved = await db.Set<Product>().FirstOrDefaultAsync(p => p.Puid == product.Puid);
        Assert.NotNull(saved);
        Assert.Equal(product.Name, saved!.Name);
    }

    [Fact]
    public async Task GetProductById_ProductExists_ReturnsProduct()
    {
        await using var db = CreateDbContext();
        var repo = new ProductRepository(db);
        var product = CreateProduct(id: 123);
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync();

        var result = await repo.GetProductById(123);

        Assert.NotNull(result);
        Assert.Equal(123, result!.Product_Id);
    }

    [Fact]
    public async Task GetProductById_ProductDoesNotExist_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new ProductRepository(db);

        var result = await repo.GetProductById(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductByPuid_ProductExists_ReturnsProduct()
    {
        await using var db = CreateDbContext();
        var repo = new ProductRepository(db);
        var product = CreateProduct(puid: "abc");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync();

        var result = await repo.GetProductByPuid("abc");

        Assert.NotNull(result);
        Assert.Equal("abc", result!.Puid);
    }

    [Fact]
    public async Task GetProductByPuid_ProductDoesNotExist_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new ProductRepository(db);

        var result = await repo.GetProductByPuid("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductsByProjectId_ProductsExist_ReturnsProductList()
    {
        await using var db = CreateDbContext();
        var repo = new ProductRepository(db);
        db.Set<Product>().AddRange(
            CreateProduct(id: 1, projectId: 10, puid: "p1"),
            CreateProduct(id: 2, projectId: 10, puid: "p2"),
            CreateProduct(id: 3, projectId: 11, puid: "p3")
        );
        await db.SaveChangesAsync();

        var result = await repo.GetProductsByProjectId(10);

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Equal(10, p.Project_Id));
    }

    [Fact]
    public async Task GetProductsByProjectId_NoProducts_ReturnsEmptyList()
    {
        await using var db = CreateDbContext();
        var repo = new ProductRepository(db);

        var result = await repo.GetProductsByProjectId(10);

        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateProduct_ExistingProduct_UpdatesDatabaseFields()
    {
        await using var db = CreateDbContext();
        var repo = new ProductRepository(db);
        var product = CreateProduct(id: 1, name: "Old");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync();

        product.Name = "New";
        product.Description = "New Desc";
        await repo.UpdateProduct(product);

        var updated = await db.Set<Product>().FindAsync(1);
        Assert.Equal("New", updated!.Name);
        Assert.Equal("New Desc", updated.Description);
    }

    [Fact]
    public async Task DeleteProduct_ProductExists_ReturnsTrueAndRemovesRecord()
    {
        await using var db = CreateDbContext();
        var repo = new ProductRepository(db);
        var product = CreateProduct(id: 1);
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync();

        var result = await repo.DeleteProduct(1);

        Assert.True(result);
        var exists = await db.Set<Product>().AnyAsync(p => p.Product_Id == 1);
        Assert.False(exists);
    }

    [Fact]
    public async Task DeleteProduct_ProductDoesNotExist_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new ProductRepository(db);

        var result = await repo.DeleteProduct(999);

        Assert.False(result);
    }

    [Fact]
    public async Task PuidExists_PuidInDatabase_ReturnsTrue()
    {
        await using var db = CreateDbContext();
        var repo = new ProductRepository(db);
        db.Set<Product>().Add(CreateProduct(puid: "exists"));
        await db.SaveChangesAsync();

        var result = await repo.PuidExists("exists");

        Assert.True(result);
    }

    [Fact]
    public async Task PuidExists_PuidNotInDatabase_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new ProductRepository(db);

        var result = await repo.PuidExists("missing");

        Assert.False(result);
    }
}
