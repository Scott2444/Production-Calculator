using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

[ExcludeFromCodeCoverage]
public class ModifierAttributeRepositoryTests
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

    private static ModifierAttribute CreateModifierAttribute(int id = 1, int modifierId = 1, int attributeId = 1)
    {
        return new ModifierAttribute
        {
            Modifier_Attribute_Id = id,
            Modifier_Id = modifierId,
            Attribute_Id = attributeId,
            Flat_Bonus = 1,
            Percent_Bonus = 2,
            Multiplicative_Bonus = 1.1,
            Version = 1,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task DeleteModifierAttribute_Missing_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new ModifierAttributeRepository(db);

        var result = await repo.DeleteModifierAttribute(999);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteModifierAttribute_Exists_ReturnsTrueAndDeletes()
    {
        await using var db = CreateDbContext();
        var repo = new ModifierAttributeRepository(db);
        db.Set<ModifierAttribute>().Add(CreateModifierAttribute(id: 12, modifierId: 20, attributeId: 5));
        await db.SaveChangesAsync();

        var result = await repo.DeleteModifierAttribute(12);

        Assert.True(result);
        Assert.Null(await db.Set<ModifierAttribute>().FindAsync(12));
    }

    [Fact]
    public async Task DeleteModifierAttributes_MixedIds_ReturnsPerIdResultAndDeletesExisting()
    {
        await using var db = CreateDbContext();
        var repo = new ModifierAttributeRepository(db);
        db.Set<ModifierAttribute>().Add(CreateModifierAttribute(id: 21));
        db.Set<ModifierAttribute>().Add(CreateModifierAttribute(id: 22));
        await db.SaveChangesAsync();

        var results = await repo.DeleteModifierAttributes(new List<int> { 21, 999, 22 });

        Assert.Equal(new List<bool> { true, false, true }, results);
        Assert.Empty(await db.Set<ModifierAttribute>().ToListAsync());
    }

    [Fact]
    public async Task AddModifierAttributes_New_AddsToDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new ModifierAttributeRepository(db);

        await repo.AddModifierAttributes(new List<ModifierAttribute> { CreateModifierAttribute(id: 0, modifierId: 10, attributeId: 9) });

        var saved = await db.Set<ModifierAttribute>().ToListAsync();
        Assert.Single(saved);
    }

    [Fact]
    public async Task UpdateModifierAttributes_Existing_UpdatesDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new ModifierAttributeRepository(db);
        var existing = CreateModifierAttribute(id: 1, modifierId: 10, attributeId: 9);
        db.Set<ModifierAttribute>().Add(existing);
        await db.SaveChangesAsync();

        existing.Flat_Bonus = 11;
        await repo.UpdateModifierAttributes(new List<ModifierAttribute> { existing });

        var saved = await db.Set<ModifierAttribute>().FindAsync(1);
        Assert.NotNull(saved);
        Assert.Equal(11, saved!.Flat_Bonus);
    }

    [Fact]
    public async Task GetByModifierId_ReturnsMatchingRows()
    {
        await using var db = CreateDbContext();
        var repo = new ModifierAttributeRepository(db);
        db.Set<ModifierAttribute>().Add(CreateModifierAttribute(id: 1, modifierId: 10, attributeId: 1));
        db.Set<ModifierAttribute>().Add(CreateModifierAttribute(id: 2, modifierId: 10, attributeId: 2));
        db.Set<ModifierAttribute>().Add(CreateModifierAttribute(id: 3, modifierId: 11, attributeId: 3));
        await db.SaveChangesAsync();

        var result = await repo.GetByModifierId(10);

        Assert.Equal(2, result.Count());
    }
}
