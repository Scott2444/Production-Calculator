using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

public class AttributeRepositoryTests
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

    private static ProjectAttribute CreateAttribute(int id = 1, int projectId = 1, string puid = "attrPuid", string name = "Attribute")
    {
        return new ProjectAttribute
        {
            Attribute_Id = id,
            Project_Id = projectId,
            Puid = puid,
            Name = name,
            Description = "desc",
            Unit = "u",
            Version = 1,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task AddAttribute_ValidAttribute_AddsAttributeToDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new AttributeRepository(db);
        var attribute = CreateAttribute(id: 0);

        await repo.AddAttribute(attribute);

        var saved = await db.Set<ProjectAttribute>().FirstOrDefaultAsync(a => a.Puid == attribute.Puid);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task GetAttributesByProjectId_AttributesExist_ReturnsList()
    {
        await using var db = CreateDbContext();
        var repo = new AttributeRepository(db);
        db.Set<ProjectAttribute>().Add(CreateAttribute(id: 1, projectId: 10, puid: "a1"));
        db.Set<ProjectAttribute>().Add(CreateAttribute(id: 2, projectId: 10, puid: "a2"));
        db.Set<ProjectAttribute>().Add(CreateAttribute(id: 3, projectId: 11, puid: "a3"));
        await db.SaveChangesAsync();

        var result = await repo.GetAttributesByProjectId(10);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task DeleteAttribute_AttributeDoesNotExist_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new AttributeRepository(db);

        var result = await repo.DeleteAttribute(999);

        Assert.False(result);
    }
}
