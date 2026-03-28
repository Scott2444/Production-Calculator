using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

[ExcludeFromCodeCoverage]
public class MachineRepositoryTests
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

    private static Machine CreateMachine(int id = 1, int projectId = 1, string puid = "machPuid", string name = "Machine", double baseSpeed = 10.0)
    {
        return new Machine
        {
            Machine_Id = id,
            Project_Id = projectId,
            Puid = puid,
            Name = name,
            Description = "desc",
            Base_Speed = baseSpeed,
            Version = 1,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task AddMachine_ValidMachine_AddsMachineToDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new MachineRepository(db);
        var machine = CreateMachine(id: 0); // EF will assign id

        await repo.AddMachine(machine);

        var saved = await db.Set<Machine>().FirstOrDefaultAsync(m => m.Puid == machine.Puid);
        Assert.NotNull(saved);
        Assert.Equal(machine.Name, saved!.Name);
    }

    [Fact]
    public async Task GetMachineById_MachineExists_ReturnsMachine()
    {
        await using var db = CreateDbContext();
        var repo = new MachineRepository(db);
        var machine = CreateMachine(id: 123);
        db.Set<Machine>().Add(machine);
        await db.SaveChangesAsync();

        var result = await repo.GetMachineById(123);

        Assert.NotNull(result);
        Assert.Equal(123, result!.Machine_Id);
    }

    [Fact]
    public async Task GetMachineById_MachineDoesNotExist_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new MachineRepository(db);

        var result = await repo.GetMachineById(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMachineByPuid_MachineExists_ReturnsMachine()
    {
        await using var db = CreateDbContext();
        var repo = new MachineRepository(db);
        var machine = CreateMachine(puid: "abc");
        db.Set<Machine>().Add(machine);
        await db.SaveChangesAsync();

        var result = await repo.GetMachineByPuid("abc");

        Assert.NotNull(result);
        Assert.Equal("abc", result!.Puid);
    }

    [Fact]
    public async Task GetMachineByPuid_MachineDoesNotExist_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new MachineRepository(db);

        var result = await repo.GetMachineByPuid("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMachinesByProjectId_MachinesExist_ReturnsMachineList()
    {
        await using var db = CreateDbContext();
        var repo = new MachineRepository(db);
        db.Set<Machine>().Add(CreateMachine(id: 1, projectId: 10, puid: "m1"));
        db.Set<Machine>().Add(CreateMachine(id: 2, projectId: 10, puid: "m2"));
        db.Set<Machine>().Add(CreateMachine(id: 3, projectId: 11, puid: "m3"));
        await db.SaveChangesAsync();

        var result = await repo.GetMachinesByProjectId(10);

        Assert.Equal(2, result.Count);
        Assert.All(result, m => Assert.Equal(10, m.Project_Id));
    }

    [Fact]
    public async Task GetMachinesByProjectId_NoMachines_ReturnsEmptyList()
    {
        await using var db = CreateDbContext();
        var repo = new MachineRepository(db);

        var result = await repo.GetMachinesByProjectId(99);

        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateMachine_ExistingMachine_UpdatesDatabaseFields()
    {
        await using var db = CreateDbContext();
        var repo = new MachineRepository(db);
        var machine = CreateMachine(id: 1, name: "Old Name");
        db.Set<Machine>().Add(machine);
        await db.SaveChangesAsync();

        machine.Name = "New Name";
        await repo.UpdateMachine(machine);

        var saved = await db.Set<Machine>().FindAsync(1);
        Assert.Equal("New Name", saved!.Name);
    }

    [Fact]
    public async Task DeleteMachine_MachineExists_ReturnsTrueAndRemovesRecord()
    {
        await using var db = CreateDbContext();
        var repo = new MachineRepository(db);
        var machine = CreateMachine(id: 123);
        db.Set<Machine>().Add(machine);
        await db.SaveChangesAsync();

        var result = await repo.DeleteMachine(123);

        Assert.True(result);
        var saved = await db.Set<Machine>().FindAsync(123);
        Assert.Null(saved);
    }

    [Fact]
    public async Task DeleteMachine_MachineDoesNotExist_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new MachineRepository(db);

        var result = await repo.DeleteMachine(999);

        Assert.False(result);
    }

    [Fact]
    public async Task PuidExists_PuidInDatabase_ReturnsTrue()
    {
        await using var db = CreateDbContext();
        var repo = new MachineRepository(db);
        db.Set<Machine>().Add(CreateMachine(puid: "exists"));
        await db.SaveChangesAsync();

        var result = await repo.PuidExists("exists");

        Assert.True(result);
    }

    [Fact]
    public async Task PuidExists_PuidNotInDatabase_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new MachineRepository(db);

        var result = await repo.PuidExists("missing");

        Assert.False(result);
    }
}
