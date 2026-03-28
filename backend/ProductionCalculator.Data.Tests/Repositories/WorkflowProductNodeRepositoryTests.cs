using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

[ExcludeFromCodeCoverage]
public class WorkflowProductNodeRepositoryTests
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

    private static WorkflowProductNode CreateWorkflowProductNode(int id = 1, int workflowId = 1, int productId = 1, double calculatedRate = 1.0, double actualIn = 1.0, double actualOut = 1.0, bool isExternal = false)
    {
        return new WorkflowProductNode
        {
            Workflow_Product_Node_Id = id,
            Workflow_Id = workflowId,
            Product_Id = productId,
            Calculated_Flow_Rate = calculatedRate,
            Actual_Flow_Rate_In = actualIn,
            Actual_Flow_Rate_Out = actualOut,
            Is_External = isExternal
        };
    }

    [Fact]
    public async Task GetByWorkflowId_Untracked_ReturnsMatchingRowsAsNoTracking()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowProductNodeRepository(db);
        db.Set<WorkflowProductNode>().Add(CreateWorkflowProductNode(id: 1, workflowId: 10, productId: 100));
        db.Set<WorkflowProductNode>().Add(CreateWorkflowProductNode(id: 2, workflowId: 10, productId: 101));
        db.Set<WorkflowProductNode>().Add(CreateWorkflowProductNode(id: 3, workflowId: 11, productId: 102));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await repo.GetByWorkflowId(10, isTracked: false);

        Assert.Equal(2, result.Count);
        Assert.Empty(db.ChangeTracker.Entries<WorkflowProductNode>());
    }

    [Fact]
    public async Task GetByWorkflowId_Tracked_ReturnsTrackedRows()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowProductNodeRepository(db);
        db.Set<WorkflowProductNode>().Add(CreateWorkflowProductNode(id: 1, workflowId: 10, productId: 100));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await repo.GetByWorkflowId(10, isTracked: true);

        Assert.Single(result);
        Assert.Single(db.ChangeTracker.Entries<WorkflowProductNode>());
    }

    [Fact]
    public async Task AddWorkflowProductNodes_ValidRows_AddsToDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowProductNodeRepository(db);

        await repo.AddWorkflowProductNodes(new List<WorkflowProductNode>
        {
            CreateWorkflowProductNode(id: 0, workflowId: 10, productId: 100),
            CreateWorkflowProductNode(id: 0, workflowId: 10, productId: 101)
        });

        Assert.Equal(2, await db.Set<WorkflowProductNode>().CountAsync());
    }

    [Fact]
    public async Task UpdateWorkflowProductNodes_ExistingRows_UpdatesDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowProductNodeRepository(db);
        var row = CreateWorkflowProductNode(id: 7, workflowId: 10, productId: 100, actualOut: 3.0);
        db.Set<WorkflowProductNode>().Add(row);
        await db.SaveChangesAsync();

        row.Actual_Flow_Rate_Out = 9.0;
        await repo.UpdateWorkflowProductNodes(new List<WorkflowProductNode> { row });

        var saved = await db.Set<WorkflowProductNode>().FindAsync(7);
        Assert.NotNull(saved);
        Assert.Equal(9.0, saved!.Actual_Flow_Rate_Out);
    }

    [Fact]
    public async Task DeleteWorkflowProductNodes_ExistingRows_ReturnsTrueAndDeletes()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowProductNodeRepository(db);
        db.Set<WorkflowProductNode>().Add(CreateWorkflowProductNode(id: 20));
        db.Set<WorkflowProductNode>().Add(CreateWorkflowProductNode(id: 21));
        await db.SaveChangesAsync();

        var result = await repo.DeleteWorkflowProductNodes(new List<int> { 20, 21 });

        Assert.True(result);
        Assert.Empty(await db.Set<WorkflowProductNode>().ToListAsync());
    }

    [Fact]
    public async Task DeleteWorkflowProductNodes_NoMatchingRows_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowProductNodeRepository(db);

        var result = await repo.DeleteWorkflowProductNodes(new List<int> { 999, 1000 });

        Assert.False(result);
    }
}