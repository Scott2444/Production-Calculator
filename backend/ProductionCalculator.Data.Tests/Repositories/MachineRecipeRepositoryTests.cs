using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

[ExcludeFromCodeCoverage]
public class MachineRecipeRepositoryTests
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

    private static MachineRecipe CreateMachineRecipe(int id = 1, int machineId = 1, int recipeId = 1)
    {
        return new MachineRecipe
        {
            Machine_Recipe_Id = id,
            Machine_Id = machineId,
            Recipe_Id = recipeId
        };
    }

    [Fact]
    public async Task GetById_MachineRecipeExists_ReturnsMachineRecipe()
    {
        await using var db = CreateDbContext();
        var repo = new MachineRecipeRepository(db);
        db.Set<MachineRecipe>().Add(CreateMachineRecipe(id: 5, machineId: 10, recipeId: 100));
        await db.SaveChangesAsync();

        var result = await repo.GetById(5);

        Assert.NotNull(result);
        Assert.Equal(5, result!.Machine_Recipe_Id);
    }

    [Fact]
    public async Task GetById_MachineRecipeDoesNotExist_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new MachineRecipeRepository(db);

        var result = await repo.GetById(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByMachineId_MachineRecipesExist_ReturnsMatchingRows()
    {
        await using var db = CreateDbContext();
        var repo = new MachineRecipeRepository(db);
        db.Set<MachineRecipe>().Add(CreateMachineRecipe(id: 1, machineId: 10, recipeId: 100));
        db.Set<MachineRecipe>().Add(CreateMachineRecipe(id: 2, machineId: 10, recipeId: 101));
        db.Set<MachineRecipe>().Add(CreateMachineRecipe(id: 3, machineId: 11, recipeId: 102));
        await db.SaveChangesAsync();

        var result = (await repo.GetByMachineId(10)).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, row => Assert.Equal(10, row.Machine_Id));
    }

    [Fact]
    public async Task GetByMachineId_NoMachineRecipes_ReturnsEmptyList()
    {
        await using var db = CreateDbContext();
        var repo = new MachineRecipeRepository(db);

        var result = (await repo.GetByMachineId(999)).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public async Task AddMachineRecipes_ValidMachineRecipes_AddsToDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new MachineRecipeRepository(db);

        await repo.AddMachineRecipes(new List<MachineRecipe>
        {
            CreateMachineRecipe(id: 0, machineId: 10, recipeId: 100),
            CreateMachineRecipe(id: 0, machineId: 10, recipeId: 101)
        });

        var saved = await db.Set<MachineRecipe>().ToListAsync();
        Assert.Equal(2, saved.Count);
    }

    [Fact]
    public async Task DeleteMachineRecipe_MachineRecipeExists_ReturnsTrueAndDeletes()
    {
        await using var db = CreateDbContext();
        var repo = new MachineRecipeRepository(db);
        db.Set<MachineRecipe>().Add(CreateMachineRecipe(id: 12, machineId: 10, recipeId: 200));
        await db.SaveChangesAsync();

        var result = await repo.DeleteMachineRecipe(12);

        Assert.True(result);
        Assert.Null(await db.Set<MachineRecipe>().FindAsync(12));
    }

    [Fact]
    public async Task DeleteMachineRecipe_MachineRecipeDoesNotExist_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new MachineRecipeRepository(db);

        var result = await repo.DeleteMachineRecipe(999);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteMachineRecipes_MixedIds_ReturnsPerIdResultAndDeletesExisting()
    {
        await using var db = CreateDbContext();
        var repo = new MachineRecipeRepository(db);
        db.Set<MachineRecipe>().Add(CreateMachineRecipe(id: 21, machineId: 10, recipeId: 201));
        db.Set<MachineRecipe>().Add(CreateMachineRecipe(id: 22, machineId: 10, recipeId: 202));
        await db.SaveChangesAsync();

        var results = await repo.DeleteMachineRecipes(new List<int> { 21, 999, 22 });

        Assert.Equal(new List<bool> { true, false, true }, results);
        Assert.Empty(await db.Set<MachineRecipe>().ToListAsync());
    }
}