using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

[ExcludeFromCodeCoverage]
public class ModifierRepositoryTests
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

    private static Modifier CreateModifier(int id = 1, int projectId = 1, string puid = "modPuid", string name = "Modifier")
    {
        return new Modifier
        {
            Modifier_Id = id,
            Project_Id = projectId,
            Puid = puid,
            Name = name,
            Description = "desc",
            Flat_Bonus = 1.0,
            Percent_Bonus = 0.5,
            Multiplicative_Bonus = 1.1,
            Input_Percent = 1.0,
            Output_Percent = 1.0,
            Version = 1,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task AddModifier_ValidModifier_AddsModifierToDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new ModifierRepository(db);
        var modifier = CreateModifier(id: 0);

        await repo.AddModifier(modifier);

        var saved = await db.Set<Modifier>().FirstOrDefaultAsync(m => m.Puid == modifier.Puid);
        Assert.NotNull(saved);
        Assert.Equal(modifier.Name, saved!.Name);
    }

    [Fact]
    public async Task GetModifierById_ModifierExists_ReturnsModifier()
    {
        await using var db = CreateDbContext();
        var repo = new ModifierRepository(db);
        var modifier = CreateModifier(id: 123);
        db.Set<Modifier>().Add(modifier);
        await db.SaveChangesAsync();

        var result = await repo.GetModifierById(123);

        Assert.NotNull(result);
        Assert.Equal(123, result!.Modifier_Id);
    }

    [Fact]
    public async Task GetModifierById_ModifierDoesNotExist_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new ModifierRepository(db);

        var result = await repo.GetModifierById(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetModifierByPuid_ModifierExists_ReturnsModifier()
    {
        await using var db = CreateDbContext();
        var repo = new ModifierRepository(db);
        var modifier = CreateModifier(puid: "abc");
        db.Set<Modifier>().Add(modifier);
        await db.SaveChangesAsync();

        var result = await repo.GetModifierByPuid("abc");

        Assert.NotNull(result);
        Assert.Equal("abc", result!.Puid);
    }

    [Fact]
    public async Task GetModifierByPuid_ModifierDoesNotExist_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new ModifierRepository(db);

        var result = await repo.GetModifierByPuid("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetModifiersByProjectId_ModifiersExist_ReturnsModifierList()
    {
        await using var db = CreateDbContext();
        var repo = new ModifierRepository(db);
        db.Set<Modifier>().Add(CreateModifier(id: 1, projectId: 10, puid: "m1"));
        db.Set<Modifier>().Add(CreateModifier(id: 2, projectId: 10, puid: "m2"));
        db.Set<Modifier>().Add(CreateModifier(id: 3, projectId: 11, puid: "m3"));
        await db.SaveChangesAsync();

        var result = await repo.GetModifiersByProjectId(10);

        Assert.Equal(2, result.Count);
        Assert.All(result, m => Assert.Equal(10, m.Project_Id));
    }

    [Fact]
    public async Task GetModifiersByProjectId_NoModifiers_ReturnsEmptyList()
    {
        await using var db = CreateDbContext();
        var repo = new ModifierRepository(db);

        var result = await repo.GetModifiersByProjectId(99);

        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateModifier_ExistingModifier_UpdatesDatabaseFields()
    {
        await using var db = CreateDbContext();
        var repo = new ModifierRepository(db);
        var modifier = CreateModifier(id: 1, name: "Old Name");
        db.Set<Modifier>().Add(modifier);
        await db.SaveChangesAsync();

        modifier.Name = "New Name";
        await repo.UpdateModifier(modifier);

        var updated = await db.Set<Modifier>().FindAsync(1);
        Assert.Equal("New Name", updated!.Name);
    }

    [Fact]
    public async Task DeleteModifier_ModifierExists_ReturnsTrueAndRemovesRecord()
    {
        await using var db = CreateDbContext();
        var repo = new ModifierRepository(db);
        db.Set<Modifier>().Add(CreateModifier(id: 1));
        await db.SaveChangesAsync();

        var result = await repo.DeleteModifier(1);

        Assert.True(result);
        Assert.Null(await db.Set<Modifier>().FindAsync(1));
    }

    [Fact]
    public async Task DeleteModifier_ModifierDoesNotExist_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new ModifierRepository(db);

        var result = await repo.DeleteModifier(999);

        Assert.False(result);
    }

    [Fact]
    public async Task PuidExists_PuidInDatabase_ReturnsTrue()
    {
        await using var db = CreateDbContext();
        var repo = new ModifierRepository(db);
        db.Set<Modifier>().Add(CreateModifier(puid: "taken"));
        await db.SaveChangesAsync();

        var result = await repo.PuidExists("taken");

        Assert.True(result);
    }

    [Fact]
    public async Task PuidExists_PuidNotInDatabase_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new ModifierRepository(db);

        var result = await repo.PuidExists("available");

        Assert.False(result);
    }
}
