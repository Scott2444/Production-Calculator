using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

public class RecipeRepositoryTests
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

    private static Recipe CreateRecipe(int id = 1, int projectId = 1, string puid = "recipePuid", string name = "Recipe")
    {
        return new Recipe
        {
            Recipe_Id = id,
            Project_Id = projectId,
            Puid = puid,
            Name = name,
            Description = "desc",
            Base_Crafting_Time = 1.0,
            Version = 1,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task AddRecipe_ValidRecipe_AddsRecipeToDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeRepository(db);
        var recipe = CreateRecipe(id: 0);

        await repo.AddRecipe(recipe);

        var saved = await db.Set<Recipe>().FirstOrDefaultAsync(r => r.Puid == recipe.Puid);
        Assert.NotNull(saved);
        Assert.Equal(recipe.Name, saved!.Name);
    }

    [Fact]
    public async Task GetByPuid_RecipeExists_ReturnsRecipe()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeRepository(db);
        var recipe = CreateRecipe(puid: "abc");
        db.Set<Recipe>().Add(recipe);
        await db.SaveChangesAsync();

        var result = await repo.GetByPuid("abc");

        Assert.NotNull(result);
        Assert.Equal("abc", result!.Puid);
    }

    [Fact]
    public async Task GetByPuid_RecipeDoesNotExist_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeRepository(db);

        var result = await repo.GetByPuid("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetById_RecipeExists_ReturnsRecipe()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeRepository(db);
        var recipe = CreateRecipe(id: 123);
        db.Set<Recipe>().Add(recipe);
        await db.SaveChangesAsync();

        var result = await repo.GetById(123);

        Assert.NotNull(result);
        Assert.Equal(123, result!.Recipe_Id);
    }

    [Fact]
    public async Task GetById_RecipeDoesNotExist_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeRepository(db);

        var result = await repo.GetById(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByProjectId_RecipesExist_ReturnsRecipeList()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeRepository(db);
        db.Set<Recipe>().AddRange(new List<Recipe>
        {
            CreateRecipe(id: 1, projectId: 10, puid: "r1"),
            CreateRecipe(id: 2, projectId: 10, puid: "r2"),
            CreateRecipe(id: 3, projectId: 20, puid: "r3")
        });
        await db.SaveChangesAsync();

        var result = await repo.GetByProjectId(10);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(10, r.Project_Id));
    }

    [Fact]
    public async Task GetByProjectId_NoRecipes_ReturnsEmptyList()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeRepository(db);

        var result = await repo.GetByProjectId(999);

        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateRecipe_ExistingRecipe_UpdatesDatabaseFields()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeRepository(db);
        var recipe = CreateRecipe();
        db.Set<Recipe>().Add(recipe);
        await db.SaveChangesAsync();

        recipe.Name = "Updated Name";
        recipe.Base_Crafting_Time = 5.0;

        await repo.UpdateRecipe(recipe);

        var saved = await db.Set<Recipe>().FindAsync(recipe.Recipe_Id);
        Assert.Equal("Updated Name", saved!.Name);
        Assert.Equal(5.0, saved.Base_Crafting_Time);
    }

    [Fact]
    public async Task DeleteRecipe_RecipeExists_ReturnsTrueAndRemovesRecord()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeRepository(db);
        var recipe = CreateRecipe(id: 456);
        db.Set<Recipe>().Add(recipe);
        await db.SaveChangesAsync();

        var result = await repo.DeleteRecipe(456);

        Assert.True(result);
        Assert.Null(await db.Set<Recipe>().FindAsync(456));
    }

    [Fact]
    public async Task DeleteRecipe_RecipeDoesNotExist_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeRepository(db);

        var result = await repo.DeleteRecipe(999);

        Assert.False(result);
    }

    [Fact]
    public async Task PuidExists_PuidInDatabase_ReturnsTrue()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeRepository(db);
        db.Set<Recipe>().Add(CreateRecipe(puid: "puid123"));
        await db.SaveChangesAsync();

        var result = await repo.PuidExists("puid123");

        Assert.True(result);
    }

    [Fact]
    public async Task PuidExists_PuidNotInDatabase_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new RecipeRepository(db);

        var result = await repo.PuidExists("missing");

        Assert.False(result);
    }
}
