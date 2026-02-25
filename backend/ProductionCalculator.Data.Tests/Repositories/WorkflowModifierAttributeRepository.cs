using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

public class WorkflowModifierAttributeRepositoryTests
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
        var repo = new WorkflowModifierAttributeRepository(db);
        db.Set<WorkflowModifierAttribute>().AddRange(
            new WorkflowModifierAttribute { Workflow_Modifier_Attribute_Id = 1, Workflow_Node_Id = 10, Workflow_Node_Modifier_Id = 100, Modifier_Id = 1, Attribute_Id = 1, Flat_Bonus = 1, Percent_Bonus = 2, Multiplicative_Bonus = 3 },
            new WorkflowModifierAttribute { Workflow_Modifier_Attribute_Id = 2, Workflow_Node_Id = 10, Workflow_Node_Modifier_Id = 101, Modifier_Id = 2, Attribute_Id = 2, Flat_Bonus = 1, Percent_Bonus = 2, Multiplicative_Bonus = 3 },
            new WorkflowModifierAttribute { Workflow_Modifier_Attribute_Id = 3, Workflow_Node_Id = 11, Workflow_Node_Modifier_Id = 102, Modifier_Id = 3, Attribute_Id = 3, Flat_Bonus = 1, Percent_Bonus = 2, Multiplicative_Bonus = 3 }
        );
        await db.SaveChangesAsync();

        var result = await repo.GetByNodeId(10);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task AddUpdateDelete_Roundtrip_Works()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowModifierAttributeRepository(db);

        var created = new WorkflowModifierAttribute
        {
            Workflow_Modifier_Attribute_Id = 0,
            Workflow_Node_Id = 10,
            Workflow_Node_Modifier_Id = 100,
            Modifier_Id = 1,
            Attribute_Id = 1,
            Flat_Bonus = 1,
            Percent_Bonus = 2,
            Multiplicative_Bonus = 3
        };

        await repo.AddWorkflowModifierAttributes([created]);
        Assert.Single(await db.Set<WorkflowModifierAttribute>().ToListAsync());

        created.Percent_Bonus = 9.0;
        await repo.UpdateWorkflowModifierAttributes([created]);
        var updated = await db.Set<WorkflowModifierAttribute>().FirstAsync();
        Assert.Equal(9.0, updated.Percent_Bonus);

        var deleted = await repo.DeleteWorkflowModifierAttributes([updated.Workflow_Modifier_Attribute_Id]);
        Assert.True(deleted);
        Assert.Empty(await db.Set<WorkflowModifierAttribute>().ToListAsync());
    }
}
