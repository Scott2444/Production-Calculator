using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

public class WorkflowRepositoryTests
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

    private static Workflow CreateWorkflow(int id = 1, int projectId = 1, string puid = "wfPuid", string name = "Workflow")
    {
        return new Workflow
        {
            Workflow_Id = id,
            Project_Id = projectId,
            Puid = puid,
            Name = name,
            Description = "desc",
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task AddWorkflow_ValidWorkflow_AddsWorkflowToDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowRepository(db);
        var workflow = CreateWorkflow(id: 0); // EF will assign id

        await repo.AddWorkflow(workflow);

        var saved = await db.Set<Workflow>().FirstOrDefaultAsync(w => w.Puid == workflow.Puid);
        Assert.NotNull(saved);
        Assert.Equal(workflow.Name, saved!.Name);
    }

    [Fact]
    public async Task GetWorkflowById_WorkflowExists_ReturnsWorkflow()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowRepository(db);
        var workflow = CreateWorkflow(id: 123);
        db.Set<Workflow>().Add(workflow);
        await db.SaveChangesAsync();

        var result = await repo.GetWorkflowById(123);

        Assert.NotNull(result);
        Assert.Equal(123, result!.Workflow_Id);
    }

    [Fact]
    public async Task GetWorkflowById_WorkflowDoesNotExist_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowRepository(db);

        var result = await repo.GetWorkflowById(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetWorkflowByPuid_WorkflowExists_ReturnsWorkflow()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowRepository(db);
        var workflow = CreateWorkflow(puid: "abc");
        db.Set<Workflow>().Add(workflow);
        await db.SaveChangesAsync();

        var result = await repo.GetWorkflowByPuid("abc");

        Assert.NotNull(result);
        Assert.Equal("abc", result!.Puid);
    }

    [Fact]
    public async Task GetWorkflowByPuid_WorkflowDoesNotExist_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowRepository(db);

        var result = await repo.GetWorkflowByPuid("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetWorkflowsByProjectId_WorkflowsExist_ReturnsWorkflowList()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowRepository(db);
        db.Set<Workflow>().AddRange(
            CreateWorkflow(id: 1, projectId: 10, puid: "w1"),
            CreateWorkflow(id: 2, projectId: 10, puid: "w2"),
            CreateWorkflow(id: 3, projectId: 20, puid: "w3")
        );
        await db.SaveChangesAsync();

        var result = await repo.GetWorkflowsByProjectId(10);

        Assert.Equal(2, result.Count);
        Assert.All(result, w => Assert.Equal(10, w.Project_Id));
    }

    [Fact]
    public async Task GetWorkflowsByProjectId_NoWorkflows_ReturnsEmptyList()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowRepository(db);

        var result = await repo.GetWorkflowsByProjectId(999);

        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateWorkflow_ExistingWorkflow_UpdatesDatabaseFields()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowRepository(db);
        var workflow = CreateWorkflow(id: 1, name: "Old Name");
        db.Set<Workflow>().Add(workflow);
        await db.SaveChangesAsync();

        workflow.Name = "New Name";
        await repo.UpdateWorkflow(workflow);

        var updated = await db.Set<Workflow>().FindAsync(1);
        Assert.Equal("New Name", updated!.Name);
    }

    [Fact]
    public async Task DeleteWorkflow_WorkflowExists_ReturnsTrueAndRemovesRecord()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowRepository(db);
        var workflow = CreateWorkflow(id: 1);
        db.Set<Workflow>().Add(workflow);
        await db.SaveChangesAsync();

        var result = await repo.DeleteWorkflow(1);

        Assert.True(result);
        var deleted = await db.Set<Workflow>().FindAsync(1);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteWorkflow_WorkflowDoesNotExist_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowRepository(db);

        var result = await repo.DeleteWorkflow(999);

        Assert.False(result);
    }

    [Fact]
    public async Task PuidExists_PuidInDatabase_ReturnsTrue()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowRepository(db);
        var workflow = CreateWorkflow(puid: "exists");
        db.Set<Workflow>().Add(workflow);
        await db.SaveChangesAsync();

        var result = await repo.PuidExists("exists");

        Assert.True(result);
    }

    [Fact]
    public async Task PuidExists_PuidNotInDatabase_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowRepository(db);

        var result = await repo.PuidExists("missing");

        Assert.False(result);
    }
}
