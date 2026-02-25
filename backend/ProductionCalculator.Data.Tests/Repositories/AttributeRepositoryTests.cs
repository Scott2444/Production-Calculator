using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;
using System.Diagnostics.CodeAnalysis;

namespace ProductionCalculator.Data.Tests;

[ExcludeFromCodeCoverage]
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
    public async Task GetAttributeById_AttributeExists_ReturnsAttribute()
    {
        await using var db = CreateDbContext();
        var repo = new AttributeRepository(db);
        var attribute = CreateAttribute(id: 5);
        db.Set<ProjectAttribute>().Add(attribute);
        await db.SaveChangesAsync();

        var result = await repo.GetAttributeById(5);

        Assert.NotNull(result);
        Assert.Equal(5, result!.Attribute_Id);
    }

    [Fact]
    public async Task GetAttributeByPuid_AttributeExists_ReturnsAttribute()
    {
        await using var db = CreateDbContext();
        var repo = new AttributeRepository(db);
        var attribute = CreateAttribute(puid: "uniquePuid");
        db.Set<ProjectAttribute>().Add(attribute);
        await db.SaveChangesAsync();

        var result = await repo.GetAttributeByPuid("uniquePuid");

        Assert.NotNull(result);
        Assert.Equal("uniquePuid", result!.Puid);
    }

    [Fact]
    public async Task UpdateAttribute_ValidAttribute_UpdatesAttribute()
    {
        await using var db = CreateDbContext();
        var repo = new AttributeRepository(db);
        var attribute = CreateAttribute(id: 7, name: "OldName");
        db.Set<ProjectAttribute>().Add(attribute);
        await db.SaveChangesAsync();

        attribute.Name = "NewName";
        var updated = await repo.UpdateAttribute(attribute);

        Assert.Equal("NewName", updated.Name);
        var fromDb = await db.Set<ProjectAttribute>().FindAsync(7);
        Assert.Equal("NewName", fromDb!.Name);
    }

    [Fact]
    public async Task DeleteAttribute_AttributeExists_ReturnsTrueAndDeletes()
    {
        await using var db = CreateDbContext();
        var repo = new AttributeRepository(db);
        var attribute = CreateAttribute(id: 8);
        db.Set<ProjectAttribute>().Add(attribute);
        await db.SaveChangesAsync();

        var result = await repo.DeleteAttribute(8);

        Assert.True(result);
        var fromDb = await db.Set<ProjectAttribute>().FindAsync(8);
        Assert.Null(fromDb);
    }

    [Fact]
    public async Task PuidExists_AttributeExists_ReturnsTrue()
    {
        await using var db = CreateDbContext();
        var repo = new AttributeRepository(db);
        var attribute = CreateAttribute(puid: "existsPuid");
        db.Set<ProjectAttribute>().Add(attribute);
        await db.SaveChangesAsync();

        var result = await repo.PuidExists("existsPuid");

        Assert.True(result);
    }

    [Fact]
    public async Task PuidExists_AttributeDoesNotExist_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new AttributeRepository(db);

        var result = await repo.PuidExists("missingPuid");

        Assert.False(result);
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
