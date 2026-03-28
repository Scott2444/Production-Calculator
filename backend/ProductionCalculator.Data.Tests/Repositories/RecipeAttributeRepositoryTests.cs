using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

[ExcludeFromCodeCoverage]
public class RecipeAttributeRepositoryTests
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

    private static RecipeAttribute CreateRecipeAttribute(int id = 1, int recipeId = 1, int attributeId = 1, double rate = 1.0)
    {
        return new RecipeAttribute
        {
            Recipe_Attribute_Id = id,
            Recipe_Id = recipeId,
            Attribute_Id = attributeId,
            Rate = rate,
            Version = 1,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task AddRecipeAttributes_New_AddsToDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeAttributeRepository(db);

        await repo.AddRecipeAttributes(new List<RecipeAttribute> { CreateRecipeAttribute(id: 0, recipeId: 10, attributeId: 9, rate: 2.5) });

        var saved = await db.Set<RecipeAttribute>().ToListAsync();
        Assert.Single(saved);
    }

    [Fact]
    public async Task UpdateRecipeAttributes_Existing_UpdatesDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeAttributeRepository(db);
        var existing = CreateRecipeAttribute(id: 1, recipeId: 10, attributeId: 9, rate: 2.5);
        db.Set<RecipeAttribute>().Add(existing);
        await db.SaveChangesAsync();

        existing.Rate = 7.5;
        await repo.UpdateRecipeAttributes(new List<RecipeAttribute> { existing });

        var saved = await db.Set<RecipeAttribute>().FindAsync(1);
        Assert.NotNull(saved);
        Assert.Equal(7.5, saved!.Rate);
    }

    [Fact]
    public async Task GetByRecipeId_ReturnsMatchingRows()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeAttributeRepository(db);
        db.Set<RecipeAttribute>().Add(CreateRecipeAttribute(id: 1, recipeId: 10, attributeId: 1));
        db.Set<RecipeAttribute>().Add(CreateRecipeAttribute(id: 2, recipeId: 10, attributeId: 2));
        db.Set<RecipeAttribute>().Add(CreateRecipeAttribute(id: 3, recipeId: 11, attributeId: 3));
        await db.SaveChangesAsync();

        var result = await repo.GetByRecipeId(10);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetById_RecipeAttributeExists_ReturnsRecipeAttribute()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeAttributeRepository(db);
        db.Set<RecipeAttribute>().Add(CreateRecipeAttribute(id: 5, recipeId: 10, attributeId: 3));
        await db.SaveChangesAsync();

        var result = await repo.GetById(5);

        Assert.NotNull(result);
        Assert.Equal(5, result!.Recipe_Attribute_Id);
    }

    [Fact]
    public async Task GetById_RecipeAttributeDoesNotExist_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeAttributeRepository(db);

        var result = await repo.GetById(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteRecipeAttribute_RecipeAttributeExists_ReturnsTrueAndDeletes()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeAttributeRepository(db);
        db.Set<RecipeAttribute>().Add(CreateRecipeAttribute(id: 12));
        await db.SaveChangesAsync();

        var result = await repo.DeleteRecipeAttribute(12);

        Assert.True(result);
        Assert.Null(await db.Set<RecipeAttribute>().FindAsync(12));
    }

    [Fact]
    public async Task DeleteRecipeAttribute_RecipeAttributeDoesNotExist_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeAttributeRepository(db);

        var result = await repo.DeleteRecipeAttribute(999);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteRecipeAttributes_MixedIds_ReturnsPerIdResultAndDeletesExisting()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeAttributeRepository(db);
        db.Set<RecipeAttribute>().Add(CreateRecipeAttribute(id: 21));
        db.Set<RecipeAttribute>().Add(CreateRecipeAttribute(id: 22));
        await db.SaveChangesAsync();

        var results = await repo.DeleteRecipeAttributes(new List<int> { 21, 999, 22 });

        Assert.Equal(new List<bool> { true, false, true }, results);
        Assert.Empty(await db.Set<RecipeAttribute>().ToListAsync());
    }
}
