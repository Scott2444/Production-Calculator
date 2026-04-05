using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

[ExcludeFromCodeCoverage]
public class ProjectRepositoryTests
{
    private static ProductionCalculatorDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ProductionCalculatorDbContext>()
            .UseInMemoryDatabase(databaseName: $"pc-tests-{Guid.NewGuid()}" )
            .Options;

        var db = new ProductionCalculatorDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static Project CreateProject(int id = 1, int userId = 1, string? puid = null, string name = "Test Project", string? aliasPuid = null, int aliasCount = 0, bool isPublic = false, DateTime? createdAt = null)
    {
        return new Project
        {
            Project_Id = id,
            User_Id = userId,
            Puid = puid ?? $"puid{id}",
            Name = name,
            Description = "Description",
            Is_Public = isPublic,
            Alias_Project_Puid = aliasPuid,
            Alias_Count = aliasCount,
            Created_At = createdAt ?? DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task AddProject_AddsProjectToDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new ProjectRepository(db);
        var project = CreateProject(id: 1);

        await repo.AddProject(project);

        var result = await db.Set<Project>().FindAsync(1);
        Assert.NotNull(result);
        Assert.Equal("Test Project", result!.Name);
    }

    [Fact]
    public async Task UpdateProject_UpdatesProjectInDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new ProjectRepository(db);
        var project = CreateProject(id: 1);
        db.Set<Project>().Add(project);
        await db.SaveChangesAsync();

        project.Name = "Updated Name";
        await repo.UpdateProject(project);

        var result = await db.Set<Project>().FindAsync(1);
        Assert.Equal("Updated Name", result!.Name);
    }

    [Fact]
    public async Task GetProjectById_ProjectExists_ReturnsProject()
    {
        await using var db = CreateDbContext();
        var repo = new ProjectRepository(db);
        var project = CreateProject(id: 1);
        db.Set<Project>().Add(project);
        await db.SaveChangesAsync();

        var result = await repo.GetProjectById(1);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Project_Id);
    }

    [Fact]
    public async Task GetProjectById_ProjectDoesNotExist_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new ProjectRepository(db);

        var result = await repo.GetProjectById(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProjectByPuid_ProjectExists_ReturnsProject()
    {
        await using var db = CreateDbContext();
        var repo = new ProjectRepository(db);
        var project = CreateProject(id: 1, puid: "test-puid");
        db.Set<Project>().Add(project);
        await db.SaveChangesAsync();

        var result = await repo.GetProjectByPuid("test-puid");

        Assert.NotNull(result);
        Assert.Equal("test-puid", result!.Puid);
    }

    [Fact]
    public async Task GetProjectByPuid_ProjectDoesNotExist_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new ProjectRepository(db);

        var result = await repo.GetProjectByPuid("non-existent");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProjectsByUserId_ProjectsExist_ReturnsList()
    {
        await using var db = CreateDbContext();
        var repo = new ProjectRepository(db);
        db.Set<Project>().Add(CreateProject(id: 1, userId: 10));
        db.Set<Project>().Add(CreateProject(id: 2, userId: 10));
        db.Set<Project>().Add(CreateProject(id: 3, userId: 20));
        await db.SaveChangesAsync();

        var result = await repo.GetProjectsByUserId(10);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetProjectsByUserId_NoProjects_ReturnsEmptyList()
    {
        await using var db = CreateDbContext();
        var repo = new ProjectRepository(db);

        var result = await repo.GetProjectsByUserId(10);

        Assert.Empty(result);
    }

    [Fact]
    public async Task DeleteProject_ProjectExists_RemovesProjectAndReturnsTrue()
    {
        await using var db = CreateDbContext();
        var repo = new ProjectRepository(db);
        db.Set<Project>().Add(CreateProject(id: 1));
        await db.SaveChangesAsync();

        var result = await repo.DeleteProject(1);

        Assert.True(result);
        Assert.Null(await db.Set<Project>().FindAsync(1));
    }

    [Fact]
    public async Task DeleteProject_ProjectDoesNotExist_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new ProjectRepository(db);

        var result = await repo.DeleteProject(999);

        Assert.False(result);
    }

    [Fact]
    public async Task PuidExists_PuidExists_ReturnsTrue()
    {
        await using var db = CreateDbContext();
        var repo = new ProjectRepository(db);
        db.Set<Project>().Add(CreateProject(id: 1, puid: "exists"));
        await db.SaveChangesAsync();

        var result = await repo.PuidExists("exists");

        Assert.True(result);
    }

    [Fact]
    public async Task PuidExists_PuidDoesNotExist_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new ProjectRepository(db);

        var result = await repo.PuidExists("does-not-exist");

        Assert.False(result);
    }

    [Fact]
    public async Task IncrementAliasCount_ProjectExists_IncrementsCount()
    {
        await using var db = CreateDbContext();
        var repo = new ProjectRepository(db);
        db.Set<Project>().Add(CreateProject(id: 1, puid: "alias", aliasCount: 0));
        await db.SaveChangesAsync();

        await repo.IncrementAliasCount("alias");

        var result = await repo.GetProjectByPuid("alias");
        Assert.NotNull(result);
        Assert.Equal(1, result!.Alias_Count);
    }

    [Fact]
    public async Task DecrementAliasCount_ProjectExists_DecrementsWithoutGoingBelowZero()
    {
        await using var db = CreateDbContext();
        var repo = new ProjectRepository(db);
        db.Set<Project>().Add(CreateProject(id: 1, puid: "alias", aliasCount: 1));
        await db.SaveChangesAsync();

        await repo.DecrementAliasCount("alias");
        await repo.DecrementAliasCount("alias");

        var result = await repo.GetProjectByPuid("alias");
        Assert.NotNull(result);
        Assert.Equal(0, result!.Alias_Count);
    }

    [Fact]
    public async Task SearchPublicProjects_NonPostgresProvider_ThrowsNotSupported()
    {
        await using var db = CreateDbContext();
        var repo = new ProjectRepository(db);

        await Assert.ThrowsAsync<NotSupportedException>(() => repo.SearchPublicProjects("search", 1, 20));
    }

    [Fact]
    public async Task GetOldestAliasOfProject_ProjectExists_ReturnsOldestAlias()
    {
        await using var db = CreateDbContext();
        var repo = new ProjectRepository(db);
        db.Set<Project>().Add(CreateProject(id: 1, puid: "canonical"));
        db.Set<Project>().Add(CreateProject(id: 2, puid: "alias1", aliasPuid: "canonical", createdAt: DateTime.UtcNow.AddHours(-1)));
        db.Set<Project>().Add(CreateProject(id: 3, puid: "alias2", aliasPuid: "canonical", createdAt: DateTime.UtcNow));
        await db.SaveChangesAsync();


        var result = await repo.GetOldestAliasOfProject("canonical");

        Assert.NotNull(result);
        Assert.Equal("alias1", result.Puid);
    }

    [Fact]
    public async Task GetOldestAliasOfProject_NoAliases_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new ProjectRepository(db);
        db.Set<Project>().Add(CreateProject(id: 1, puid: "canonical"));
        await db.SaveChangesAsync();

        var result = await repo.GetOldestAliasOfProject("canonical");

        Assert.Null(result);
    }
}
