using System.Diagnostics.CodeAnalysis;
using FakeItEasy;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Services;
using Microsoft.Extensions.Logging;
using ProductionCalculator.Business.APIModels;

namespace ProductionCalculator.Business.Tests;

[ExcludeFromCodeCoverage]
public class UserServiceTests
{
    private readonly IUserRepository _repo;
    private readonly IRoleRepository _roleRepo;
    private readonly ILogger<UserService> _logger;
    private readonly IProjectService _projectService;
    private readonly IAuthService _authService;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _repo = A.Fake<IUserRepository>();
        _roleRepo = A.Fake<IRoleRepository>();
        _logger = A.Fake<ILogger<UserService>>();
        _projectService = A.Fake<IProjectService>();
        _authService = A.Fake<IAuthService>();
        _service = new UserService(_repo, _roleRepo, _logger, _projectService, _authService);
    }

    private User CreateTestUser(string username = "test", string email = "test@test.com", string puid = "user123456", int roleId = 1)
    {
        return new User
        {
            User_Id = 1,
            Username = username,
            Email = email,
            Password_Hash = "hash",
            Role_Id = roleId,
            Puid = puid,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    private Role CreateTestRole(int id = 1, string name = "User")
    {
        return new Role
        {
            Role_Id = id,
            Role_Name = name
        };
    }

    [Theory]
    [InlineData("", "email@test.com", "password123")]
    [InlineData("user", "", "password123")]
    [InlineData("user", "email@test.com", "")]
    [InlineData("us", "email@test.com", "password123")] // Too short
    [InlineData("thisusernameiswaytoolongtobevalid", "email@test.com", "password123")] // Too long
    [InlineData("user!", "email@test.com", "password123")] // Invalid chars
    [InlineData("user", "email@test.com", "short")] // Password too short
    [InlineData("user", "email@test.com", "thispasswordiswaytoooolongtobevalid123")] // Password too long
    public async Task Register_InvalidInput_ReturnsBadRequest(string username, string email, string password)
    {
        // Act
        var result = await _service.Register(username, email, password);

        // Assert
        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task Register_ExistingUsername_ReturnsConflict()
    {
        // Arrange
        A.CallTo(() => _repo.GetByUsername("existing")).Returns(CreateTestUser(username: "existing"));

        // Act
        var result = await _service.Register("existing", "email@test.com", "password123");

        // Assert
        Assert.Equal(ServiceStatus.Conflict409, result.Status);
    }

    [Fact]
    public async Task Register_ExistingEmail_ReturnsConflict()
    {
        // Arrange
        A.CallTo(() => _repo.GetByUsername(A<string>._)).Returns(Task.FromResult<User?>(null));
        A.CallTo(() => _repo.GetByEmail("existing@test.com")).Returns(CreateTestUser(email: "existing@test.com"));

        // Act
        var result = await _service.Register("newuser", "existing@test.com", "password123");

        // Assert
        Assert.Equal(ServiceStatus.Conflict409, result.Status);
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsCreated()
    {
        // Arrange
        A.CallTo(() => _repo.GetByUsername(A<string>._)).Returns(Task.FromResult<User?>(null));
        A.CallTo(() => _repo.GetByEmail(A<string>._)).Returns(Task.FromResult<User?>(null));
        A.CallTo(() => _repo.PuidExists(A<string>._)).Returns(false);

        // Act
        var result = await _service.Register("validuser", "valid@test.com", "password123");

        // Assert
        Assert.Equal(ServiceStatus.Created201, result.Status);
        Assert.NotNull(result.Data);
        Assert.Equal("validuser", result.Data.Username);
        A.CallTo(() => _repo.AddUser(A<User>._)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData("", "email@test.com")]
    [InlineData("user", "")]
    public async Task ValidateNewUser_EmptyInput_ReturnsBadRequest(string username, string email)
    {
        // Act
        var result = await _service.ValidateNewUser(username, email);

        // Assert
        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task ValidateNewUser_ExistingUsername_ReturnsConflict()
    {
        // Arrange
        A.CallTo(() => _repo.GetByUsername("existing")).Returns(CreateTestUser(username: "existing"));

        // Act
        var result = await _service.ValidateNewUser("existing", "email@test.com");

        // Assert
        Assert.Equal(ServiceStatus.Conflict409, result.Status);
    }

    [Fact]
    public async Task ValidateNewUser_ExistingEmail_ReturnsConflict()
    {
        // Arrange
        A.CallTo(() => _repo.GetByUsername(A<string>._)).Returns(Task.FromResult<User?>(null));
        A.CallTo(() => _repo.GetByEmail("existing@test.com")).Returns(CreateTestUser(email: "existing@test.com"));

        // Act
        var result = await _service.ValidateNewUser("newuser", "existing@test.com");

        // Assert
        Assert.Equal(ServiceStatus.Conflict409, result.Status);
    }

    [Fact]
    public async Task ValidateNewUser_ValidInput_ReturnsOk()
    {
        // Arrange
        A.CallTo(() => _repo.GetByUsername(A<string>._)).Returns(Task.FromResult<User?>(null));
        A.CallTo(() => _repo.GetByEmail(A<string>._)).Returns(Task.FromResult<User?>(null));

        // Act
        var result = await _service.ValidateNewUser("newuser", "new@test.com");

        // Assert
        Assert.Equal(ServiceStatus.Ok200, result.Status);
    }

    [Fact]
    public async Task GetUserByPuid_EmptyPuid_ReturnsBadRequest()
    {
        // Act
        var result = await _service.GetUserByPuid("");

        // Assert
        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task GetUserByPuid_NotFound_ReturnsNotFound()
    {
        // Arrange
        A.CallTo(() => _repo.GetByPuid("nonexistent")).Returns(Task.FromResult<User?>(null));

        // Act
        var result = await _service.GetUserByPuid("nonexistent");

        // Assert
        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetUserByPuid_RoleNotFound_ReturnsInternalServerError()
    {
        // Arrange
        var user = CreateTestUser(roleId: 99);
        A.CallTo(() => _repo.GetByPuid("valid")).Returns(user);
        A.CallTo(() => _roleRepo.GetRole(99)).Returns(Task.FromResult<Role?>(null));

        // Act
        var result = await _service.GetUserByPuid("valid");

        // Assert
        Assert.Equal(ServiceStatus.InternalServerError500, result.Status);
    }

    [Fact]
    public async Task GetUserByPuid_Success_ReturnsOk()
    {
        // Arrange
        var user = CreateTestUser(roleId: 1, username: "test");
        var role = CreateTestRole(id: 1, name: "User");
        A.CallTo(() => _repo.GetByPuid("valid")).Returns(user);
        A.CallTo(() => _roleRepo.GetRole(1)).Returns(role);

        // Act
        var result = await _service.GetUserByPuid("valid");

        // Assert
        Assert.Equal(ServiceStatus.Ok200, result.Status);
        Assert.True(result.Data.Item2); // Verified
        Assert.Equal("test", result.Data.Item1.Username);
    }

    [Fact]
    public async Task GetUserByUsername_Success_ReturnsOk()
    {
        // Arrange
        var user = CreateTestUser(roleId: 1, username: "test");
        var role = CreateTestRole(id: 1, name: "User");
        A.CallTo(() => _repo.GetByUsername("test")).Returns(user);
        A.CallTo(() => _roleRepo.GetRole(1)).Returns(role);

        // Act
        var result = await _service.GetUserByUsername("test");

        // Assert
        Assert.Equal(ServiceStatus.Ok200, result.Status);
        Assert.True(result.Data.Item2);
    }

    [Fact]
    public async Task DeleteUserById_NotFound_ReturnsNotFound()
    {
        // Arrange
        A.CallTo(() => _repo.GetByPuid("nonexistent")).Returns(Task.FromResult<User?>(null));

        // Act
        var result = await _service.DeleteUserById("nonexistent", "testuser", "testpassword");

        // Assert
        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task DeleteUserById_InvalidLogin_ReturnsUnauthorized()
    {
        // Arrange
        var user = CreateTestUser();
        A.CallTo(() => _repo.GetByPuid("valid")).Returns(user);
        A.CallTo(() => _authService.Login("testuser", "testpassword", false))
            .Returns((ServiceResult<AuthResponse>.Fail(ServiceStatus.Unauthorized401, "Invalid username or password."), null, null));

        // Act
        var result = await _service.DeleteUserById("valid", "testuser", "testpassword");

        // Assert
        Assert.Equal(ServiceStatus.Unauthorized401, result.Status);
    }

    [Fact]
    public async Task DeleteUserById_ProjectRetrievalFails_ReturnsInternalServerError()
    {
        // Arrange
        var user = CreateTestUser();
        A.CallTo(() => _repo.GetByPuid("valid")).Returns(user);
        A.CallTo(() => _authService.Login("testuser", "testpassword", false))
            .Returns((ServiceResult<AuthResponse>.SuccessResult(new AuthResponse { Puid = user.Puid, Username = user.Username }), null, null));
        A.CallTo(() => _projectService.GetProjectsByUserPuid(user.Puid)).Returns(Task.FromResult(ServiceResult<List<ProjectResponse>>.Fail(ServiceStatus.InternalServerError500, "Failed to retrieve projects.")));

        // Act
        var result = await _service.DeleteUserById("valid", "testuser", "testpassword");

        // Assert
        Assert.Equal(ServiceStatus.InternalServerError500, result.Status);
    }

    [Fact]
    public async Task DeleteUserById_RepoReturnsFalse_ReturnsInternalServerError()
    {
        // Arrange
        var user = CreateTestUser();
        A.CallTo(() => _repo.GetByPuid("valid")).Returns(user);
        A.CallTo(() => _repo.DeleteUser(user.User_Id)).Returns(false);
        A.CallTo(() => _authService.Login("testuser", "testpassword", false))
            .Returns((ServiceResult<AuthResponse>.SuccessResult(new AuthResponse { Puid = user.Puid, Username = user.Username }), null, null));

        // Act
        var result = await _service.DeleteUserById("valid", "testuser", "testpassword");

        // Assert
        Assert.Equal(ServiceStatus.InternalServerError500, result.Status);
    }

    [Fact]
    public async Task DeleteUserById_Success_ReturnsNoContent()
    {
        // Arrange
        var user = CreateTestUser();
        A.CallTo(() => _repo.GetByPuid("valid")).Returns(user);
        A.CallTo(() => _repo.DeleteUser(user.User_Id)).Returns(true);
        A.CallTo(() => _projectService.GetProjectsByUserPuid(user.Puid)).Returns(Task.FromResult(ServiceResult<List<APIModels.ProjectResponse>>.SuccessResult(new List<APIModels.ProjectResponse>(), ServiceStatus.Ok200)));
        A.CallTo(() => _authService.Login("testuser", "testpassword", false))
            .Returns((ServiceResult<AuthResponse>.SuccessResult(new AuthResponse { Puid = user.Puid, Username = user.Username }), null, null));

        // Act
        var result = await _service.DeleteUserById("valid", "testuser", "testpassword");

        // Assert
        Assert.Equal(ServiceStatus.NoContent204, result.Status);
    }
}
