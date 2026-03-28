using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

[ExcludeFromCodeCoverage]
public class WorkflowRecipeRepositoryTests
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

    private static WorkflowRecipe CreateWorkflowRecipe(int id = 1, int workflowId = 1, int recipeId = 1)
    {
        return new WorkflowRecipe
        {
            Workflow_Recipe_Id = id,
            Workflow_Id = workflowId,
            Recipe_Id = recipeId
        };
    }

    [Fact]
    public async Task GetByWorkflowId_Untracked_ReturnsMatchingRowsAsNoTracking()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowRecipeRepository(db);
        db.Set<WorkflowRecipe>().Add(CreateWorkflowRecipe(id: 1, workflowId: 10, recipeId: 100));
        db.Set<WorkflowRecipe>().Add(CreateWorkflowRecipe(id: 2, workflowId: 10, recipeId: 101));
        db.Set<WorkflowRecipe>().Add(CreateWorkflowRecipe(id: 3, workflowId: 11, recipeId: 102));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await repo.GetByWorkflowId(10, isTracked: false);

        Assert.Equal(2, result.Count);
        Assert.Empty(db.ChangeTracker.Entries<WorkflowRecipe>());
    }

    [Fact]
    public async Task GetByWorkflowId_Tracked_ReturnsTrackedRows()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowRecipeRepository(db);
        db.Set<WorkflowRecipe>().Add(CreateWorkflowRecipe(id: 1, workflowId: 10, recipeId: 100));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await repo.GetByWorkflowId(10, isTracked: true);

        Assert.Single(result);
        Assert.Single(db.ChangeTracker.Entries<WorkflowRecipe>());
    }

    [Fact]
    public async Task AddWorkflowRecipes_ValidRows_AddsToDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowRecipeRepository(db);

        await repo.AddWorkflowRecipes(new List<WorkflowRecipe>
        {
            CreateWorkflowRecipe(id: 0, workflowId: 10, recipeId: 100),
            CreateWorkflowRecipe(id: 0, workflowId: 10, recipeId: 101)
        });

        Assert.Equal(2, await db.Set<WorkflowRecipe>().CountAsync());
    }

    [Fact]
    public async Task UpdateWorkflowRecipes_ExistingRows_UpdatesDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowRecipeRepository(db);
        var row = CreateWorkflowRecipe(id: 7, workflowId: 10, recipeId: 100);
        db.Set<WorkflowRecipe>().Add(row);
        await db.SaveChangesAsync();

        row.Recipe_Id = 200;
        await repo.UpdateWorkflowRecipes(new List<WorkflowRecipe> { row });

        var saved = await db.Set<WorkflowRecipe>().FindAsync(7);
        Assert.NotNull(saved);
        Assert.Equal(200, saved!.Recipe_Id);
    }

    [Fact]
    public async Task DeleteWorkflowRecipes_ExistingRows_ReturnsTrueAndDeletes()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowRecipeRepository(db);
        db.Set<WorkflowRecipe>().Add(CreateWorkflowRecipe(id: 20));
        db.Set<WorkflowRecipe>().Add(CreateWorkflowRecipe(id: 21));
        await db.SaveChangesAsync();

        var result = await repo.DeleteWorkflowRecipes(new List<int> { 20, 21 });

        Assert.True(result);
        Assert.Empty(await db.Set<WorkflowRecipe>().ToListAsync());
    }

    [Fact]
    public async Task DeleteWorkflowRecipes_NoMatchingRows_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowRecipeRepository(db);

        var result = await repo.DeleteWorkflowRecipes(new List<int> { 999, 1000 });

        Assert.False(result);
    }
}