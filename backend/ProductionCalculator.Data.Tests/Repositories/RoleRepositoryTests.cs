using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

[ExcludeFromCodeCoverage]
public class RoleRepositoryTests
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

    private static Role CreateRole(int id = 1, string name = "Admin")
    {
        return new Role
        {
            Role_Id = id,
            Role_Name = name
        };
    }

    [Fact]
    public async Task GetRole_ById_RoleExists_ReturnsRole()
    {
        await using var db = CreateDbContext();
        var repo = new RoleRepository(db);
        db.Set<Role>().Add(CreateRole(id: 2, name: "User"));
        await db.SaveChangesAsync();

        var result = await repo.GetRole(2);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Role_Id);
    }

    [Fact]
    public async Task GetRole_ById_RoleDoesNotExist_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new RoleRepository(db);

        var result = await repo.GetRole(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRole_ByName_RoleExists_ReturnsRole()
    {
        await using var db = CreateDbContext();
        var repo = new RoleRepository(db);
        db.Set<Role>().Add(CreateRole(id: 3, name: "Editor"));
        await db.SaveChangesAsync();

        var result = await repo.GetRole("Editor");

        Assert.NotNull(result);
        Assert.Equal("Editor", result!.Role_Name);
    }

    [Fact]
    public async Task GetRole_ByName_RoleDoesNotExist_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new RoleRepository(db);

        var result = await repo.GetRole("MissingRole");

        Assert.Null(result);
    }
}