using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

[ExcludeFromCodeCoverage]
public class WorkflowRecipeAttributeRepositoryTests
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
        var repo = new WorkflowRecipeAttributeRepository(db);
        db.Set<WorkflowRecipeAttribute>().AddRange(
            new WorkflowRecipeAttribute { Workflow_Recipe_Attribute_Id = 1, Workflow_Node_Id = 10, Attribute_Id = 1, Rate = 2.5 },
            new WorkflowRecipeAttribute { Workflow_Recipe_Attribute_Id = 2, Workflow_Node_Id = 10, Attribute_Id = 2, Rate = 3.5 },
            new WorkflowRecipeAttribute { Workflow_Recipe_Attribute_Id = 3, Workflow_Node_Id = 11, Attribute_Id = 1, Rate = 4.5 }
        );
        await db.SaveChangesAsync();

        var result = await repo.GetByNodeId(10);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task AddUpdateDelete_Roundtrip_Works()
    {
        await using var db = CreateDbContext();
        var repo = new WorkflowRecipeAttributeRepository(db);

        var created = new WorkflowRecipeAttribute { Workflow_Recipe_Attribute_Id = 0, Workflow_Node_Id = 10, Attribute_Id = 1, Rate = 1.0 };
        await repo.AddWorkflowRecipeAttributes([created]);
        Assert.Single(await db.Set<WorkflowRecipeAttribute>().ToListAsync());

        created.Rate = 9.0;
        await repo.UpdateWorkflowRecipeAttributes([created]);
        var updated = await db.Set<WorkflowRecipeAttribute>().FirstAsync();
        Assert.Equal(9.0, updated.Rate);

        var deleted = await repo.DeleteWorkflowRecipeAttributes([updated.Workflow_Recipe_Attribute_Id]);
        Assert.True(deleted);
        Assert.Empty(await db.Set<WorkflowRecipeAttribute>().ToListAsync());
    }
}
