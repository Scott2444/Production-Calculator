using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests;

[ExcludeFromCodeCoverage]
public class UserRepositoryTests
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

    private static User CreateUser(int id = 1, string? puid = null, string? username = null, string? email = null, int projectCount = 0)
    {
        return new User
        {
            User_Id = id,
            Username = username ?? $"user{id}",
            Email = email ?? $"user{id}@example.com",
            Password_Hash = $"hash{id}",
            Role_Id = 1,
            Puid = puid ?? $"puid{id}",
            Project_Count = projectCount,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetById_UserExists_ReturnsUser()
    {
        await using var db = CreateDbContext();
        var repo = new UserRepository(db);
        var user = CreateUser(id: 123);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await repo.GetById(123);

        Assert.NotNull(result);
        Assert.Equal(123, result!.User_Id);
    }

    [Fact]
    public async Task GetById_UserDoesNotExist_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new UserRepository(db);

        var result = await repo.GetById(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByPuid_UserExists_ReturnsUser()
    {
        await using var db = CreateDbContext();
        var repo = new UserRepository(db);
        var user = CreateUser(id: 1, puid: "abc");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await repo.GetByPuid("abc");

        Assert.NotNull(result);
        Assert.Equal("abc", result!.Puid);
    }

    [Fact]
    public async Task GetByPuid_UserDoesNotExist_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new UserRepository(db);

        var result = await repo.GetByPuid("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUsername_UserExists_ReturnsUser()
    {
        await using var db = CreateDbContext();
        var repo = new UserRepository(db);
        var user = CreateUser(id: 1, username: "TestUser");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await repo.GetByUsername("testuser");

        Assert.NotNull(result);
        Assert.Equal("TestUser", result!.Username);
    }

    [Fact]
    public async Task GetByUsername_UserDoesNotExist_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new UserRepository(db);

        var result = await repo.GetByUsername("nope");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmail_UserExists_ReturnsUser()
    {
        await using var db = CreateDbContext();
        var repo = new UserRepository(db);
        var user = CreateUser(id: 1, email: "x@y.com");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await repo.GetByEmail("x@y.com");

        Assert.NotNull(result);
        Assert.Equal("x@y.com", result!.Email);
    }

    [Fact]
    public async Task GetByEmail_UserDoesNotExist_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new UserRepository(db);

        var result = await repo.GetByEmail("missing@y.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task AddUser_ValidUser_AddsUserToDatabase()
    {
        await using var db = CreateDbContext();
        var repo = new UserRepository(db);
        var user = CreateUser(id: 1);

        await repo.AddUser(user);

        var found = await db.Users.FindAsync(1);
        Assert.NotNull(found);
        Assert.Equal(user.Username, found!.Username);
    }

    [Fact]
    public async Task UpdateUser_ExistingUser_UpdatesDatabaseFields()
    {
        await using var db = CreateDbContext();
        var repo = new UserRepository(db);
        var user = CreateUser(id: 1, username: "old");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        user.Username = "new";
        user.Last_Updated = DateTime.UtcNow.AddMinutes(1);

        await repo.UpdateUser(user);

        var found = await db.Users.FindAsync(1);
        Assert.NotNull(found);
        Assert.Equal("new", found!.Username);
    }

    [Fact]
    public async Task DeleteUser_UserExists_ReturnsTrueAndRemovesRecord()
    {
        await using var db = CreateDbContext();
        var repo = new UserRepository(db);
        db.Users.Add(CreateUser(id: 1));
        await db.SaveChangesAsync();

        var deleted = await repo.DeleteUser(1);

        Assert.True(deleted);
        Assert.Null(await db.Users.FindAsync(1));
    }

    [Fact]
    public async Task DeleteUser_UserDoesNotExist_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new UserRepository(db);

        var deleted = await repo.DeleteUser(42);

        Assert.False(deleted);
    }

    [Fact]
    public async Task GetPasswordHash_UserExists_ReturnsHashString()
    {
        await using var db = CreateDbContext();
        var repo = new UserRepository(db);
        var user = CreateUser(id: 1);
        user.Password_Hash = "myhash";
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var hash = await repo.GetPasswordHash(1);

        Assert.Equal("myhash", hash);
    }

    [Fact]
    public async Task PuidExists_PuidInDatabase_ReturnsTrue()
    {
        await using var db = CreateDbContext();
        var repo = new UserRepository(db);
        db.Users.Add(CreateUser(id: 1, puid: "exists"));
        await db.SaveChangesAsync();

        var exists = await repo.PuidExists("exists");

        Assert.True(exists);
    }

    [Fact]
    public async Task PuidExists_PuidNotInDatabase_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new UserRepository(db);

        var exists = await repo.PuidExists("missing");

        Assert.False(exists);
    }

    [Fact]
    public async Task ProjectCountMethods_RespectLimitAndDoNotGoBelowZero()
    {
        await using var db = CreateDbContext();
        var repo = new UserRepository(db);
        db.Users.Add(CreateUser(id: 1, puid: "userPuid", projectCount: 0));
        await db.SaveChangesAsync();

        Assert.True(await repo.TryIncrementProjectCount("userPuid", 1));
        Assert.False(await repo.TryIncrementProjectCount("userPuid", 1));

        await repo.IncrementProjectCount("userPuid");
        await repo.DecrementProjectCount("userPuid");
        await repo.DecrementProjectCount("userPuid");
        await repo.DecrementProjectCount("userPuid");

        var user = await repo.GetByPuid("userPuid");
        Assert.NotNull(user);
        Assert.Equal(0, user!.Project_Count);
    }

    [Fact]
    public async Task IsRegistrationEnabled_NoSettingsRow_ReturnsTrue()
    {
        await using var db = CreateDbContext();
        var repo = new UserRepository(db);

        var enabled = await repo.IsRegistrationEnabled();

        Assert.True(enabled);
    }

    [Fact]
    public async Task SetRegistrationEnabled_NoSettingsRow_CreatesSettingsRow()
    {
        await using var db = CreateDbContext();
        var repo = new UserRepository(db);

        await repo.SetRegistrationEnabled(false);

        var enabled = await repo.IsRegistrationEnabled();
        var settings = await db.Set<RegistrationSetting>().SingleAsync();

        Assert.False(enabled);
        Assert.Equal(1, settings.Settings_Id);
        Assert.False(settings.Is_Registration_Enabled);
    }

    [Fact]
    public async Task SetRegistrationEnabled_SettingsRowExists_UpdatesValue()
    {
        await using var db = CreateDbContext();
        db.Set<RegistrationSetting>().Add(new RegistrationSetting
        {
            Settings_Id = 1,
            Is_Registration_Enabled = false,
            Last_Updated = DateTime.UtcNow.AddHours(-1)
        });
        await db.SaveChangesAsync();

        var repo = new UserRepository(db);
        await repo.SetRegistrationEnabled(true);

        var enabled = await repo.IsRegistrationEnabled();
        Assert.True(enabled);
    }
}
