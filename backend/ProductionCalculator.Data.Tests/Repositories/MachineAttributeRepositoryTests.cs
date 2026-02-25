using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

[ExcludeFromCodeCoverage]
public class MachineAttributeRepositoryTests
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

    private static MachineAttribute CreateMachineAttribute(int id = 1, int machineId = 1, int attributeId = 1, double rate = 1.0)
    {
        return new MachineAttribute
        {
            Machine_Attribute_Id = id,
            Machine_Id = machineId,
            Attribute_Id = attributeId,
            Rate = rate,
            Version = 1,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task AddMachineAttributes_New_AddsToDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new MachineAttributeRepository(db);

        await repo.AddMachineAttributes(new List<MachineAttribute> { CreateMachineAttribute(id: 0, machineId: 10, attributeId: 9, rate: 2.5) });

        var saved = await db.Set<MachineAttribute>().ToListAsync();
        Assert.Single(saved);
    }

    [Fact]
    public async Task UpdateMachineAttributes_Existing_UpdatesDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new MachineAttributeRepository(db);
        var existing = CreateMachineAttribute(id: 1, machineId: 10, attributeId: 9, rate: 2.5);
        db.Set<MachineAttribute>().Add(existing);
        await db.SaveChangesAsync();

        existing.Rate = 7.5;
        await repo.UpdateMachineAttributes(new List<MachineAttribute> { existing });

        var saved = await db.Set<MachineAttribute>().FindAsync(1);
        Assert.NotNull(saved);
        Assert.Equal(7.5, saved!.Rate);
    }

    [Fact]
    public async Task DeleteMachineAttribute_Missing_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new MachineAttributeRepository(db);

        var result = await repo.DeleteMachineAttribute(999);

        Assert.False(result);
    }
}
