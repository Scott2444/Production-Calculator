using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests.Repositories;

public class RefreshTokenRepositoryTests
{
    private static ProductionCalculatorDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ProductionCalculatorDbContext>()
            .UseInMemoryDatabase(databaseName: $"rt-tests-{Guid.NewGuid()}")
            .Options;

        var db = new ProductionCalculatorDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task AddRefreshToken_ValidToken_AddsToDatabase()
    {
        // Arrange
        var db = CreateDbContext();
        var repo = new RefreshTokenRepository(db);
        var token = new RefreshToken { Token_Id = Guid.NewGuid(), Token = "token1", User_Id = 1, Created_At = DateTime.UtcNow, Expires_At = DateTime.UtcNow.AddDays(1) };

        // Act
        await repo.AddRefreshToken(token);

        // Assert
        var result = await db.Set<RefreshToken>().FindAsync(token.Token_Id);
        Assert.NotNull(result);
        Assert.Equal("token1", result.Token);
    }

    [Fact]
    public async Task GetRefreshTokenById_TokenExists_ReturnsToken()
    {
        // Arrange
        var db = CreateDbContext();
        var token = new RefreshToken { Token_Id = Guid.NewGuid(), Token = "token1", User_Id = 1, Created_At = DateTime.UtcNow, Expires_At = DateTime.UtcNow.AddDays(1) };
        await db.Set<RefreshToken>().AddAsync(token);
        await db.SaveChangesAsync();
        var repo = new RefreshTokenRepository(db);

        // Act
        var result = await repo.GetRefreshTokenById(token.Token_Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(token.Token_Id, result.Token_Id);
    }

    [Fact]
    public async Task GetRefreshTokenById_TokenDoesNotExist_ReturnsNull()
    {
        // Arrange
        var db = CreateDbContext();
        var repo = new RefreshTokenRepository(db);

        // Act
        var result = await repo.GetRefreshTokenById(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetRefreshTokenByToken_TokenExists_ReturnsToken()
    {
        // Arrange
        var db = CreateDbContext();
        var token = new RefreshToken { Token_Id = Guid.NewGuid(), Token = "token1", User_Id = 1, Created_At = DateTime.UtcNow, Expires_At = DateTime.UtcNow.AddDays(1) };
        await db.Set<RefreshToken>().AddAsync(token);
        await db.SaveChangesAsync();
        var repo = new RefreshTokenRepository(db);

        // Act
        var result = await repo.GetRefreshTokenByToken("token1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(token.Token_Id, result.Token_Id);
    }

    [Fact]
    public async Task GetRefreshTokenByToken_TokenDoesNotExist_ReturnsNull()
    {
        // Arrange
        var db = CreateDbContext();
        var repo = new RefreshTokenRepository(db);

        // Act
        var result = await repo.GetRefreshTokenByToken("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetRefreshTokensByUserId_UserHasTokens_ReturnsList()
    {
        // Arrange
        var db = CreateDbContext();
        var userId = 1;
        await db.Set<RefreshToken>().AddRangeAsync(
            new RefreshToken { Token_Id = Guid.NewGuid(), Token = "t1", User_Id = userId, Created_At = DateTime.UtcNow, Expires_At = DateTime.UtcNow.AddDays(1) },
            new RefreshToken { Token_Id = Guid.NewGuid(), Token = "t2", User_Id = userId, Created_At = DateTime.UtcNow, Expires_At = DateTime.UtcNow.AddDays(1) },
            new RefreshToken { Token_Id = Guid.NewGuid(), Token = "t3", User_Id = 2, Created_At = DateTime.UtcNow, Expires_At = DateTime.UtcNow.AddDays(1) }
        );
        await db.SaveChangesAsync();
        var repo = new RefreshTokenRepository(db);

        // Act
        var result = await repo.GetRefreshTokensByUserId(userId);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetRefreshTokensByUserId_UserHasNoTokens_ReturnsEmptyList()
    {
        // Arrange
        var db = CreateDbContext();
        var repo = new RefreshTokenRepository(db);

        // Act
        var result = await repo.GetRefreshTokensByUserId(1);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateRefreshToken_ValidToken_UpdatesDatabase()
    {
        // Arrange
        var db = CreateDbContext();
        var token = new RefreshToken { Token_Id = Guid.NewGuid(), Token = "token1", User_Id = 1, Created_At = DateTime.UtcNow, Expires_At = DateTime.UtcNow.AddDays(1) };
        await db.Set<RefreshToken>().AddAsync(token);
        await db.SaveChangesAsync();
        var repo = new RefreshTokenRepository(db);

        token.Revoked_At = DateTime.UtcNow;

        // Act
        await repo.UpdateRefreshToken(token);

        // Assert
        var updated = await db.Set<RefreshToken>().FindAsync(token.Token_Id);
        Assert.NotNull(updated?.Revoked_At);
    }

    [Fact]
    public async Task DeleteRefreshToken_TokenExists_RemovesTokenAndReturnsTrue()
    {
        // Arrange
        var db = CreateDbContext();
        var token = new RefreshToken { Token_Id = Guid.NewGuid(), Token = "token1", User_Id = 1, Created_At = DateTime.UtcNow, Expires_At = DateTime.UtcNow.AddDays(1) };
        await db.Set<RefreshToken>().AddAsync(token);
        await db.SaveChangesAsync();
        var repo = new RefreshTokenRepository(db);

        // Act
        var result = await repo.DeleteRefreshToken(token.Token_Id);

        // Assert
        Assert.True(result);
        Assert.Null(await db.Set<RefreshToken>().FindAsync(token.Token_Id));
    }

    [Fact]
    public async Task DeleteRefreshToken_TokenDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var db = CreateDbContext();
        var repo = new RefreshTokenRepository(db);

        // Act
        var result = await repo.DeleteRefreshToken(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }
}
