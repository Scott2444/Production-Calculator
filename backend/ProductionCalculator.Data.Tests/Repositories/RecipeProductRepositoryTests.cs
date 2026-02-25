using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

public class RecipeProductRepositoryTests
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

    private static RecipeProduct CreateRecipeProduct(int id = 1, int recipeId = 1, int productId = 1, double quantity = 10.0, bool isInput = true)
    {
        return new RecipeProduct
        {
            Recipe_Product_Id = id,
            Recipe_Id = recipeId,
            Product_Id = productId,
            Quantity = quantity,
            Is_Input = isInput
        };
    }

    [Fact]
    public async Task GetById_RecipeProductExists_ReturnsRecipeProduct()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeProductRepository(db);
        var rp = CreateRecipeProduct(id: 123);
        db.Set<RecipeProduct>().Add(rp);
        await db.SaveChangesAsync();

        var result = await repo.GetById(123);

        Assert.NotNull(result);
        Assert.Equal(123, result!.Recipe_Product_Id);
    }

    [Fact]
    public async Task GetById_RecipeProductDoesNotExist_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeProductRepository(db);

        var result = await repo.GetById(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByRecipeId_RecipeProductsExist_ReturnsRecipeProductList()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeProductRepository(db);
        db.Set<RecipeProduct>().AddRange(new List<RecipeProduct>
        {
            CreateRecipeProduct(id: 1, recipeId: 10),
            CreateRecipeProduct(id: 2, recipeId: 10),
            CreateRecipeProduct(id: 3, recipeId: 20)
        });
        await db.SaveChangesAsync();

        var result = await repo.GetByRecipeId(10);

        Assert.Equal(2, result.Count());
        Assert.All(result, r => Assert.Equal(10, r.Recipe_Id));
    }

    [Fact]
    public async Task GetByRecipeId_NoRecipeProducts_ReturnsEmptyList()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeProductRepository(db);

        var result = await repo.GetByRecipeId(999);

        Assert.Empty(result);
    }

    [Fact]
    public async Task AddRecipeProducts_NewRecipeProduct_AddsToDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeProductRepository(db);
        var rp = CreateRecipeProduct(id: 0); // New

        await repo.AddRecipeProducts(new List<RecipeProduct> { rp });

        var saved = await db.Set<RecipeProduct>().ToListAsync();
        Assert.Single(saved);
        Assert.Equal(rp.Quantity, saved[0].Quantity);
    }

    [Fact]
    public async Task UpdateRecipeProducts_ExistingRecipeProduct_UpdatesDatabaseFields()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeProductRepository(db);
        var rp = CreateRecipeProduct(id: 111, quantity: 10.0);
        db.Set<RecipeProduct>().Add(rp);
        await db.SaveChangesAsync();

        rp.Quantity = 20.0; // Update existing object

        await repo.UpdateRecipeProducts(new List<RecipeProduct> { rp });

        var saved = await db.Set<RecipeProduct>().FindAsync(111);
        Assert.Equal(20.0, saved!.Quantity);
    }

    [Fact]
    public async Task DeleteRecipeProduct_RecipeProductExists_ReturnsTrueAndRemovesRecord()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeProductRepository(db);
        var rp = CreateRecipeProduct(id: 789);
        db.Set<RecipeProduct>().Add(rp);
        await db.SaveChangesAsync();

        var result = await repo.DeleteRecipeProduct(789);

        Assert.True(result);
        Assert.Null(await db.Set<RecipeProduct>().FindAsync(789));
    }

    [Fact]
    public async Task DeleteRecipeProduct_RecipeProductDoesNotExist_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeProductRepository(db);

        var result = await repo.DeleteRecipeProduct(999);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteRecipeProducts_MixedIds_ReturnsListOfResults()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeProductRepository(db);
        db.Set<RecipeProduct>().Add(CreateRecipeProduct(id: 1));
        db.Set<RecipeProduct>().Add(CreateRecipeProduct(id: 2));
        await db.SaveChangesAsync();

        var result = await repo.DeleteRecipeProducts(new List<int> { 1, 99, 2 });

        Assert.Equal(3, result.Count);
        Assert.True(result[0]); // ID 1 deleted
        Assert.False(result[1]); // ID 99 not found
        Assert.True(result[2]); // ID 2 deleted
    }
}
