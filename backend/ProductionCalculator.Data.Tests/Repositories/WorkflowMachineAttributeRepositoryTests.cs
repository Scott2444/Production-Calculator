using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

[ExcludeFromCodeCoverage]
public class WorkflowMachineAttributeRepositoryTests
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

    [Fact]
    public async Task GetByNodeId_ReturnsMatchingRows()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowMachineAttributeRepository(db);
        db.Set<WorkflowMachineAttribute>().AddRange(
            new WorkflowMachineAttribute { Workflow_Machine_Attribute_Id = 1, Workflow_Node_Id = 10, Attribute_Id = 1, Rate = 2.5 },
            new WorkflowMachineAttribute { Workflow_Machine_Attribute_Id = 2, Workflow_Node_Id = 10, Attribute_Id = 2, Rate = 3.5 },
            new WorkflowMachineAttribute { Workflow_Machine_Attribute_Id = 3, Workflow_Node_Id = 11, Attribute_Id = 1, Rate = 4.5 }
        );
        await db.SaveChangesAsync();

        var result = await repo.GetByNodeId(10);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task AddUpdateDelete_Roundtrip_Works()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowMachineAttributeRepository(db);

        var created = new WorkflowMachineAttribute { Workflow_Machine_Attribute_Id = 0, Workflow_Node_Id = 10, Attribute_Id = 1, Rate = 1.0 };
        await repo.AddWorkflowMachineAttributes([created]);
        Assert.Single(await db.Set<WorkflowMachineAttribute>().ToListAsync());

        created.Rate = 9.0;
        await repo.UpdateWorkflowMachineAttributes([created]);
        var updated = await db.Set<WorkflowMachineAttribute>().FirstAsync();
        Assert.Equal(9.0, updated.Rate);

        var deleted = await repo.DeleteWorkflowMachineAttributes([updated.Workflow_Machine_Attribute_Id]);
        Assert.True(deleted);
        Assert.Empty(await db.Set<WorkflowMachineAttribute>().ToListAsync());
    }
}
