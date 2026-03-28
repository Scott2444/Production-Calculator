using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

[ExcludeFromCodeCoverage]
public class WorkflowNodeModifierRepositoryTests
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

    private static WorkflowNodeModifier CreateWorkflowNodeModifier(int id = 1, int nodeId = 1, int modifierId = 1, int modifierVersion = 1)
    {
        return new WorkflowNodeModifier
        {
            Workflow_Node_Modifier_Id = id,
            Workflow_Node_Id = nodeId,
            Modifier_Id = modifierId,
            Modifier_Version = modifierVersion
        };
    }

    [Fact]
    public async Task GetByNodeId_Untracked_ReturnsMatchingRowsAsNoTracking()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowNodeModifierRepository(db);
        db.Set<WorkflowNodeModifier>().Add(CreateWorkflowNodeModifier(id: 1, nodeId: 10, modifierId: 100));
        db.Set<WorkflowNodeModifier>().Add(CreateWorkflowNodeModifier(id: 2, nodeId: 10, modifierId: 101));
        db.Set<WorkflowNodeModifier>().Add(CreateWorkflowNodeModifier(id: 3, nodeId: 11, modifierId: 102));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await repo.GetByNodeId(10);

        Assert.Equal(2, result.Count);
        Assert.Empty(db.ChangeTracker.Entries<WorkflowNodeModifier>());
    }

    [Fact]
    public async Task GetByNodeId_Tracked_ReturnsTrackedRows()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowNodeModifierRepository(db);
        db.Set<WorkflowNodeModifier>().Add(CreateWorkflowNodeModifier(id: 1, nodeId: 10, modifierId: 100));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await repo.GetByNodeId(10, isTracked: true);

        Assert.Single(result);
        Assert.Single(db.ChangeTracker.Entries<WorkflowNodeModifier>());
    }

    [Fact]
    public async Task AddWorkflowNodeModifiers_ValidRows_AddsToDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowNodeModifierRepository(db);

        await repo.AddWorkflowNodeModifiers(new List<WorkflowNodeModifier>
        {
            CreateWorkflowNodeModifier(id: 0, nodeId: 10, modifierId: 100),
            CreateWorkflowNodeModifier(id: 0, nodeId: 10, modifierId: 101)
        });

        Assert.Equal(2, await db.Set<WorkflowNodeModifier>().CountAsync());
    }

    [Fact]
    public async Task UpdateWorkflowNodeModifiers_ExistingRows_UpdatesDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowNodeModifierRepository(db);
        var row = CreateWorkflowNodeModifier(id: 7, nodeId: 10, modifierId: 100, modifierVersion: 1);
        db.Set<WorkflowNodeModifier>().Add(row);
        await db.SaveChangesAsync();

        row.Modifier_Version = 3;
        await repo.UpdateWorkflowNodeModifiers(new List<WorkflowNodeModifier> { row });

        var saved = await db.Set<WorkflowNodeModifier>().FindAsync(7);
        Assert.NotNull(saved);
        Assert.Equal(3, saved!.Modifier_Version);
    }

    [Fact]
    public async Task DeleteWorkflowNodeModifiers_ExistingRows_ReturnsTrueAndDeletes()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowNodeModifierRepository(db);
        db.Set<WorkflowNodeModifier>().Add(CreateWorkflowNodeModifier(id: 20));
        db.Set<WorkflowNodeModifier>().Add(CreateWorkflowNodeModifier(id: 21));
        await db.SaveChangesAsync();

        var result = await repo.DeleteWorkflowNodeModifiers(new List<int> { 20, 21 });

        Assert.True(result);
        Assert.Empty(await db.Set<WorkflowNodeModifier>().ToListAsync());
    }

    [Fact]
    public async Task DeleteWorkflowNodeModifiers_NoMatchingRows_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowNodeModifierRepository(db);

        var result = await repo.DeleteWorkflowNodeModifiers(new List<int> { 999, 1000 });

        Assert.False(result);
    }
}