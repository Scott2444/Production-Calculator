using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

[ExcludeFromCodeCoverage]
public class WorkflowNodeRepositoryTests
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

    private static WorkflowNode CreateWorkflowNode(int id = 1, int workflowId = 1, string puid = "nodePuid", int recipeId = 1, int recipeVersion = 1)
    {
        return new WorkflowNode
        {
            Node_Id = id,
            Workflow_Id = workflowId,
            Puid = puid,
            Recipe_Id = recipeId,
            Recipe_Version = recipeVersion,
            Machine_Id = null,
            Machine_Version = null,
            Actual_Machine_Count = null,
            Calculated_Machine_Count = null,
            Calculated_Target_Rate = null,
            Calculated_Actual_Rate = null
        };
    }

    [Fact]
    public async Task GetByWorkflow_Untracked_ReturnsMatchingRowsAsNoTracking()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowNodeRepository(db);
        db.Set<WorkflowNode>().Add(CreateWorkflowNode(id: 1, workflowId: 10, puid: "n1"));
        db.Set<WorkflowNode>().Add(CreateWorkflowNode(id: 2, workflowId: 10, puid: "n2"));
        db.Set<WorkflowNode>().Add(CreateWorkflowNode(id: 3, workflowId: 11, puid: "n3"));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await repo.GetByWorkflow(10);

        Assert.Equal(2, result.Count);
        Assert.Empty(db.ChangeTracker.Entries<WorkflowNode>());
    }

    [Fact]
    public async Task GetByWorkflow_Tracked_ReturnsTrackedRows()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowNodeRepository(db);
        db.Set<WorkflowNode>().Add(CreateWorkflowNode(id: 1, workflowId: 10, puid: "n1"));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await repo.GetByWorkflow(10, isTracked: true);

        Assert.Single(result);
        Assert.Single(db.ChangeTracker.Entries<WorkflowNode>());
    }

    [Fact]
    public async Task AddWorkflowNodes_ValidRows_AddsToDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowNodeRepository(db);

        await repo.AddWorkflowNodes(new List<WorkflowNode>
        {
            CreateWorkflowNode(id: 0, workflowId: 10, puid: "n1"),
            CreateWorkflowNode(id: 0, workflowId: 10, puid: "n2")
        });

        Assert.Equal(2, await db.Set<WorkflowNode>().CountAsync());
    }

    [Fact]
    public async Task UpdateWorkflowNodes_ExistingRows_UpdatesDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowNodeRepository(db);
        var row = CreateWorkflowNode(id: 7, workflowId: 10, puid: "node-7");
        db.Set<WorkflowNode>().Add(row);
        await db.SaveChangesAsync();

        row.Recipe_Version = 3;
        await repo.UpdateWorkflowNodes(new List<WorkflowNode> { row });

        var saved = await db.Set<WorkflowNode>().FindAsync(7);
        Assert.NotNull(saved);
        Assert.Equal(3, saved!.Recipe_Version);
    }

    [Fact]
    public async Task DeleteWorkflowNodes_ExistingRows_ReturnsTrueAndDeletes()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowNodeRepository(db);
        db.Set<WorkflowNode>().Add(CreateWorkflowNode(id: 20, puid: "n20"));
        db.Set<WorkflowNode>().Add(CreateWorkflowNode(id: 21, puid: "n21"));
        await db.SaveChangesAsync();

        var result = await repo.DeleteWorkflowNodes(new List<int> { 20, 21 });

        Assert.True(result);
        Assert.Empty(await db.Set<WorkflowNode>().ToListAsync());
    }

    [Fact]
    public async Task DeleteWorkflowNodes_NoMatchingRows_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowNodeRepository(db);

        var result = await repo.DeleteWorkflowNodes(new List<int> { 999, 1000 });

        Assert.False(result);
    }

    [Fact]
    public async Task PuidExists_PuidInDatabase_ReturnsTrue()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowNodeRepository(db);
        db.Set<WorkflowNode>().Add(CreateWorkflowNode(id: 1, workflowId: 10, puid: "exists"));
        await db.SaveChangesAsync();

        var result = await repo.PuidExists("exists");

        Assert.True(result);
    }

    [Fact]
    public async Task PuidExists_PuidNotInDatabase_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowNodeRepository(db);

        var result = await repo.PuidExists("missing");

        Assert.False(result);
    }
}