using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Tests.Repositories;

[ExcludeFromCodeCoverage]
public class VerificationCodeRepositoryTests
{
    private static ProductionCalculatorDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ProductionCalculatorDbContext>()
            .UseInMemoryDatabase(databaseName: $"vc-tests-{Guid.NewGuid()}")
            .Options;

        var db = new ProductionCalculatorDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task AddVerificationCode_ValidCode_AddsToDatabase()
    {
        // Arrange
        var db = CreateDbContext();
        var repo = new VerificationCodeRepository(db);
        var code = new VerificationCode { Code_Id = Guid.NewGuid(), User_Id = 1, Code_Hash = "hash", Created_At = DateTime.UtcNow, Expires_At = DateTime.UtcNow.AddMinutes(10), Attempts = 0 };

        // Act
        await repo.AddVerificationCode(code);

        // Assert
        var result = await db.Set<VerificationCode>().FindAsync(code.Code_Id);
        Assert.NotNull(result);
        Assert.Equal("hash", result.Code_Hash);
    }

    [Fact]
    public async Task UpdateVerificationCode_ValidCode_UpdatesDatabase()
    {
        // Arrange
        var db = CreateDbContext();
        var code = new VerificationCode { Code_Id = Guid.NewGuid(), User_Id = 1, Code_Hash = "hash", Created_At = DateTime.UtcNow, Expires_At = DateTime.UtcNow.AddMinutes(10), Attempts = 0 };
        await db.Set<VerificationCode>().AddAsync(code);
        await db.SaveChangesAsync();
        var repo = new VerificationCodeRepository(db);

        code.Attempts = 1;

        // Act
        await repo.UpdateVerificationCode(code);

        // Assert
        var updated = await db.Set<VerificationCode>().FindAsync(code.Code_Id);
        Assert.Equal(1, updated?.Attempts);
    }

    [Fact]
    public async Task GetVerificationCodeById_CodeExists_ReturnsCode()
    {
        // Arrange
        var db = CreateDbContext();
        var code = new VerificationCode { Code_Id = Guid.NewGuid(), User_Id = 1, Code_Hash = "hash", Created_At = DateTime.UtcNow, Expires_At = DateTime.UtcNow.AddMinutes(10), Attempts = 0 };
        await db.Set<VerificationCode>().AddAsync(code);
        await db.SaveChangesAsync();
        var repo = new VerificationCodeRepository(db);

        // Act
        var result = await repo.GetVerificationCodeById(code.Code_Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(code.Code_Id, result.Code_Id);
    }

    [Fact]
    public async Task GetVerificationCodeById_CodeDoesNotExist_ReturnsNull()
    {
        // Arrange
        var db = CreateDbContext();
        var repo = new VerificationCodeRepository(db);

        // Act
        var result = await repo.GetVerificationCodeById(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetVerificationCodeByCodeHash_CodeExists_ReturnsCode()
    {
        // Arrange
        var db = CreateDbContext();
        var code = new VerificationCode { Code_Id = Guid.NewGuid(), User_Id = 1, Code_Hash = "hash1", Created_At = DateTime.UtcNow, Expires_At = DateTime.UtcNow.AddMinutes(10), Attempts = 0 };
        await db.Set<VerificationCode>().AddAsync(code);
        await db.SaveChangesAsync();
        var repo = new VerificationCodeRepository(db);

        // Act
        var result = await repo.GetVerificationCodeByCodeHash("hash1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(code.Code_Id, result.Code_Id);
    }

    [Fact]
    public async Task GetVerificationCodeByCodeHash_CodeDoesNotExist_ReturnsNull()
    {
        // Arrange
        var db = CreateDbContext();
        var repo = new VerificationCodeRepository(db);

        // Act
        var result = await repo.GetVerificationCodeByCodeHash("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetVerificationCodesByUserId_UserHasCodes_ReturnsList()
    {
        // Arrange
        var db = CreateDbContext();
        var userId = 1;
        await db.Set<VerificationCode>().AddRangeAsync(
            new VerificationCode { Code_Id = Guid.NewGuid(), User_Id = userId, Code_Hash = "h1", Created_At = DateTime.UtcNow, Expires_At = DateTime.UtcNow.AddMinutes(10), Attempts = 0 },
            new VerificationCode { Code_Id = Guid.NewGuid(), User_Id = userId, Code_Hash = "h2", Created_At = DateTime.UtcNow, Expires_At = DateTime.UtcNow.AddMinutes(10), Attempts = 0 },
            new VerificationCode { Code_Id = Guid.NewGuid(), User_Id = 2, Code_Hash = "h3", Created_At = DateTime.UtcNow, Expires_At = DateTime.UtcNow.AddMinutes(10), Attempts = 0 }
        );
        await db.SaveChangesAsync();
        var repo = new VerificationCodeRepository(db);

        // Act
        var result = await repo.GetVerificationCodesByUserId(userId);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetVerificationCodesByUserId_UserHasNoCodes_ReturnsEmptyList()
    {
        // Arrange
        var db = CreateDbContext();
        var repo = new VerificationCodeRepository(db);

        // Act
        var result = await repo.GetVerificationCodesByUserId(1);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task DeleteVerificationCode_CodeExists_RemovesCodeAndReturnsTrue()
    {
        // Arrange
        var db = CreateDbContext();
        var code = new VerificationCode { Code_Id = Guid.NewGuid(), User_Id = 1, Code_Hash = "hash", Created_At = DateTime.UtcNow, Expires_At = DateTime.UtcNow.AddMinutes(10), Attempts = 0 };
        await db.Set<VerificationCode>().AddAsync(code);
        await db.SaveChangesAsync();
        var repo = new VerificationCodeRepository(db);

        // Act
        var result = await repo.DeleteVerificationCode(code.Code_Id);

        // Assert
        Assert.True(result);
        Assert.Null(await db.Set<VerificationCode>().FindAsync(code.Code_Id));
    }

    [Fact]
    public async Task DeleteVerificationCode_CodeDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var db = CreateDbContext();
        var repo = new VerificationCodeRepository(db);

        // Act
        var result = await repo.DeleteVerificationCode(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }
}
