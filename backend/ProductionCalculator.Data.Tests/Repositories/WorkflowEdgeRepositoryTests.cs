using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

[ExcludeFromCodeCoverage]
public class WorkflowEdgeRepositoryTests
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

    private static WorkflowEdge CreateWorkflowEdge(int id = 1, int workflowId = 1, int productNodeId = 1, int? producerNodeId = null, int? consumerNodeId = null, double calculatedRate = 1.0, double actualRate = 1.0)
    {
        return new WorkflowEdge
        {
            Workflow_Edge_Id = id,
            Workflow_Id = workflowId,
            Producer_Node_Id = producerNodeId,
            Consumer_Node_Id = consumerNodeId,
            Product_Node_Id = productNodeId,
            Calculated_Flow_Rate = calculatedRate,
            Actual_Flow_Rate = actualRate
        };
    }

    [Fact]
    public async Task GetByWorkflow_Untracked_ReturnsMatchingRowsAsNoTracking()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowEdgeRepository(db);
        db.Set<WorkflowEdge>().Add(CreateWorkflowEdge(id: 1, workflowId: 10, productNodeId: 101));
        db.Set<WorkflowEdge>().Add(CreateWorkflowEdge(id: 2, workflowId: 10, productNodeId: 102));
        db.Set<WorkflowEdge>().Add(CreateWorkflowEdge(id: 3, workflowId: 11, productNodeId: 103));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await repo.GetByWorkflow(10);

        Assert.Equal(2, result.Count);
        Assert.Empty(db.ChangeTracker.Entries<WorkflowEdge>());
    }

    [Fact]
    public async Task GetByWorkflow_Tracked_ReturnsTrackedRows()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowEdgeRepository(db);
        db.Set<WorkflowEdge>().Add(CreateWorkflowEdge(id: 1, workflowId: 10, productNodeId: 101));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await repo.GetByWorkflow(10, isTracked: true);

        Assert.Single(result);
        Assert.Single(db.ChangeTracker.Entries<WorkflowEdge>());
    }

    [Fact]
    public async Task AddWorkflowEdges_ValidRows_AddsToDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowEdgeRepository(db);

        await repo.AddWorkflowEdges(new List<WorkflowEdge>
        {
            CreateWorkflowEdge(id: 0, workflowId: 10, productNodeId: 100),
            CreateWorkflowEdge(id: 0, workflowId: 10, productNodeId: 101)
        });

        var saved = await db.Set<WorkflowEdge>().ToListAsync();
        Assert.Equal(2, saved.Count);
    }

    [Fact]
    public async Task UpdateWorkflowEdges_ExistingRows_UpdatesDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowEdgeRepository(db);
        var edge = CreateWorkflowEdge(id: 7, workflowId: 10, productNodeId: 100, actualRate: 2.0);
        db.Set<WorkflowEdge>().Add(edge);
        await db.SaveChangesAsync();

        edge.Actual_Flow_Rate = 9.0;
        await repo.UpdateWorkflowEdges(new List<WorkflowEdge> { edge });

        var saved = await db.Set<WorkflowEdge>().FindAsync(7);
        Assert.NotNull(saved);
        Assert.Equal(9.0, saved!.Actual_Flow_Rate);
    }

    [Fact]
    public async Task DeleteWorkflowEdges_ExistingRows_ReturnsTrueAndDeletes()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowEdgeRepository(db);
        db.Set<WorkflowEdge>().Add(CreateWorkflowEdge(id: 20, workflowId: 10, productNodeId: 100));
        db.Set<WorkflowEdge>().Add(CreateWorkflowEdge(id: 21, workflowId: 10, productNodeId: 101));
        await db.SaveChangesAsync();

        var result = await repo.DeleteWorkflowEdges(new List<int> { 20, 21 });

        Assert.True(result);
        Assert.Empty(await db.Set<WorkflowEdge>().ToListAsync());
    }

    [Fact]
    public async Task DeleteWorkflowEdges_NoMatchingRows_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowEdgeRepository(db);

        var result = await repo.DeleteWorkflowEdges(new List<int> { 999, 1000 });

        Assert.False(result);
    }
}