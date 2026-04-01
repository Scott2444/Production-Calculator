using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests.Repositories;

[ExcludeFromCodeCoverage]
public class PasswordResetTokenRepositoryTests
{
    private static ProductionCalculatorDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ProductionCalculatorDbContext>()
            .UseInMemoryDatabase(databaseName: $"password-reset-token-tests-{Guid.NewGuid()}")
            .Options;

        var db = new ProductionCalculatorDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task AddPasswordResetToken_ValidToken_AddsToDatabase()
    {
        // Arrange
        await using var db = CreateDbContext();
        var repo = new PasswordResetTokenRepository(db);
        var token = new PasswordResetToken
        {
            Reset_Id = Guid.NewGuid(),
            User_Id = 1,
            Token_Hash = "hash1",
            Created_At = DateTime.UtcNow,
            Expires_At = DateTime.UtcNow.AddMinutes(30)
        };

        // Act
        await repo.AddPasswordResetToken(token);

        // Assert
        var result = await db.Set<PasswordResetToken>().FindAsync(token.Reset_Id);
        Assert.NotNull(result);
        Assert.Equal("hash1", result!.Token_Hash);
    }

    [Fact]
    public async Task UpdatePasswordResetToken_ValidToken_UpdatesDatabase()
    {
        // Arrange
        await using var db = CreateDbContext();
        var token = new PasswordResetToken
        {
            Reset_Id = Guid.NewGuid(),
            User_Id = 1,
            Token_Hash = "hash1",
            Created_At = DateTime.UtcNow,
            Expires_At = DateTime.UtcNow.AddMinutes(30)
        };
        await db.Set<PasswordResetToken>().AddAsync(token);
        await db.SaveChangesAsync();
        var repo = new PasswordResetTokenRepository(db);

        token.Token_Hash = "hash2";

        // Act
        await repo.UpdatePasswordResetToken(token);

        // Assert
        var updated = await db.Set<PasswordResetToken>().FindAsync(token.Reset_Id);
        Assert.NotNull(updated);
        Assert.Equal("hash2", updated!.Token_Hash);
    }

    [Fact]
    public async Task GetPasswordResetTokenByUserId_TokenExists_ReturnsToken()
    {
        // Arrange
        await using var db = CreateDbContext();
        var token = new PasswordResetToken
        {
            Reset_Id = Guid.NewGuid(),
            User_Id = 42,
            Token_Hash = "hash1",
            Created_At = DateTime.UtcNow,
            Expires_At = DateTime.UtcNow.AddMinutes(30)
        };
        await db.Set<PasswordResetToken>().AddAsync(token);
        await db.SaveChangesAsync();
        var repo = new PasswordResetTokenRepository(db);

        // Act
        var result = await repo.GetPasswordResetTokenByUserId(42);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(token.Reset_Id, result!.Reset_Id);
    }

    [Fact]
    public async Task GetPasswordResetTokenByUserId_TokenMissing_ReturnsNull()
    {
        // Arrange
        await using var db = CreateDbContext();
        var repo = new PasswordResetTokenRepository(db);

        // Act
        var result = await repo.GetPasswordResetTokenByUserId(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPasswordResetTokenByTokenHash_TokenExists_ReturnsToken()
    {
        // Arrange
        await using var db = CreateDbContext();
        var token = new PasswordResetToken
        {
            Reset_Id = Guid.NewGuid(),
            User_Id = 42,
            Token_Hash = "hash1",
            Created_At = DateTime.UtcNow,
            Expires_At = DateTime.UtcNow.AddMinutes(30)
        };
        await db.Set<PasswordResetToken>().AddAsync(token);
        await db.SaveChangesAsync();
        var repo = new PasswordResetTokenRepository(db);

        // Act
        var result = await repo.GetPasswordResetTokenByTokenHash("hash1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(token.Reset_Id, result!.Reset_Id);
    }

    [Fact]
    public async Task GetPasswordResetTokenByTokenHash_TokenMissing_ReturnsNull()
    {
        // Arrange
        await using var db = CreateDbContext();
        var repo = new PasswordResetTokenRepository(db);

        // Act
        var result = await repo.GetPasswordResetTokenByTokenHash("missing");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeletePasswordResetToken_TokenExists_RemovesAndReturnsTrue()
    {
        // Arrange
        await using var db = CreateDbContext();
        var token = new PasswordResetToken
        {
            Reset_Id = Guid.NewGuid(),
            User_Id = 42,
            Token_Hash = "hash1",
            Created_At = DateTime.UtcNow,
            Expires_At = DateTime.UtcNow.AddMinutes(30)
        };
        await db.Set<PasswordResetToken>().AddAsync(token);
        await db.SaveChangesAsync();
        var repo = new PasswordResetTokenRepository(db);

        // Act
        var deleted = await repo.DeletePasswordResetToken(token.Reset_Id);

        // Assert
        Assert.True(deleted);
        Assert.Null(await db.Set<PasswordResetToken>().FindAsync(token.Reset_Id));
    }

    [Fact]
    public async Task DeletePasswordResetToken_TokenMissing_ReturnsFalse()
    {
        // Arrange
        await using var db = CreateDbContext();
        var repo = new PasswordResetTokenRepository(db);

        // Act
        var deleted = await repo.DeletePasswordResetToken(Guid.NewGuid());

        // Assert
        Assert.False(deleted);
    }
}