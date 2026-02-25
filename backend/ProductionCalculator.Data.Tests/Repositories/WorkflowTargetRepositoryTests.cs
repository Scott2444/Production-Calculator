using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

[ExcludeFromCodeCoverage]
public class WorkflowTargetRepositoryTests
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

    private static WorkflowTarget CreateWorkflowTarget(int id = 1, int workflowId = 1, int productId = 1, double targetRate = 1.0)
    {
        return new WorkflowTarget
        {
            Workflow_Target_Id = id,
            Workflow_Id = workflowId,
            Product_Id = productId,
            Target_Rate = targetRate
        };
    }

    [Fact]
    public async Task GetByWorkflowId_Untracked_ReturnsMatchingRowsAsNoTracking()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowTargetRepository(db);
        db.Set<WorkflowTarget>().Add(CreateWorkflowTarget(id: 1, workflowId: 10, productId: 100));
        db.Set<WorkflowTarget>().Add(CreateWorkflowTarget(id: 2, workflowId: 10, productId: 101));
        db.Set<WorkflowTarget>().Add(CreateWorkflowTarget(id: 3, workflowId: 11, productId: 102));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await repo.GetByWorkflowId(10, isTracked: false);

        Assert.Equal(2, result.Count);
        Assert.Empty(db.ChangeTracker.Entries<WorkflowTarget>());
    }

    [Fact]
    public async Task GetByWorkflowId_Tracked_ReturnsTrackedRows()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowTargetRepository(db);
        db.Set<WorkflowTarget>().Add(CreateWorkflowTarget(id: 1, workflowId: 10, productId: 100));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await repo.GetByWorkflowId(10, isTracked: true);

        Assert.Single(result);
        Assert.Single(db.ChangeTracker.Entries<WorkflowTarget>());
    }

    [Fact]
    public async Task AddWorkflowTargets_ValidRows_AddsToDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowTargetRepository(db);

        await repo.AddWorkflowTargets(new List<WorkflowTarget>
        {
            CreateWorkflowTarget(id: 0, workflowId: 10, productId: 100, targetRate: 5),
            CreateWorkflowTarget(id: 0, workflowId: 10, productId: 101, targetRate: 10)
        });

        Assert.Equal(2, await db.Set<WorkflowTarget>().CountAsync());
    }

    [Fact]
    public async Task UpdateWorkflowTargets_ExistingRows_UpdatesDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowTargetRepository(db);
        var row = CreateWorkflowTarget(id: 7, workflowId: 10, productId: 100, targetRate: 3.0);
        db.Set<WorkflowTarget>().Add(row);
        await db.SaveChangesAsync();

        row.Target_Rate = 9.0;
        await repo.UpdateWorkflowTargets(new List<WorkflowTarget> { row });

        var saved = await db.Set<WorkflowTarget>().FindAsync(7);
        Assert.NotNull(saved);
        Assert.Equal(9.0, saved!.Target_Rate);
    }

    [Fact]
    public async Task DeleteWorkflowTargets_ExistingRows_ReturnsTrueAndDeletes()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowTargetRepository(db);
        db.Set<WorkflowTarget>().Add(CreateWorkflowTarget(id: 20));
        db.Set<WorkflowTarget>().Add(CreateWorkflowTarget(id: 21));
        await db.SaveChangesAsync();

        var result = await repo.DeleteWorkflowTargets(new List<int> { 20, 21 });

        Assert.True(result);
        Assert.Empty(await db.Set<WorkflowTarget>().ToListAsync());
    }

    [Fact]
    public async Task DeleteWorkflowTargets_NoMatchingRows_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowTargetRepository(db);

        var result = await repo.DeleteWorkflowTargets(new List<int> { 999, 1000 });

        Assert.False(result);
    }
}