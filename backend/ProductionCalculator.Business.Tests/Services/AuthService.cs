using FakeItEasy;
using Microsoft.Extensions.Configuration;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Services;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Helpers;
using Resend;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using FakeItEasy.Sdk;

namespace ProductionCalculator.Business.Tests.Services;

public class AuthServiceTests
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IProjectRepository _projectRepository;
    private readonly IVerificationCodeRepository _verificationCodeRepository;
    private readonly IResend _resend;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly JwtHelper _jwtHelper;
    private readonly RefreshTokenHelper _refreshTokenHelper;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _currentUserService = A.Fake<ICurrentUserService>();
        _userRepo = A.Fake<IUserRepository>();
        _roleRepo = A.Fake<IRoleRepository>();
        _refreshTokenRepo = A.Fake<IRefreshTokenRepository>();
        _projectRepository = A.Fake<IProjectRepository>();
        _verificationCodeRepository = A.Fake<IVerificationCodeRepository>();
        _resend = A.Fake<IResend>();
        _httpContextAccessor = A.Fake<IHttpContextAccessor>();
        _configuration = A.Fake<IConfiguration>();

        // Setup config for helpers - Use explicit section mocking for reliability
        var jwtSection = A.Fake<IConfigurationSection>();
        A.CallTo(() => jwtSection["Key"]).Returns("SecretKeyForTesting1234567890123");
        A.CallTo(() => jwtSection["ExpireMinutes"]).Returns("60");
        A.CallTo(() => jwtSection["Issuer"]).Returns("issuer");
        A.CallTo(() => jwtSection["Audience"]).Returns("audience");
        A.CallTo(() => _configuration.GetSection("Jwt")).Returns(jwtSection);

        var rfSection = A.Fake<IConfigurationSection>();
        A.CallTo(() => rfSection["ExpireDays"]).Returns("7");
        A.CallTo(() => rfSection["MaxSessions"]).Returns("5");
        A.CallTo(() => _configuration.GetSection("RefreshToken")).Returns(rfSection);

        var vcSection = A.Fake<IConfigurationSection>();
        A.CallTo(() => vcSection["MaxRequests"]).Returns("3");
        A.CallTo(() => vcSection["ExpireMinutes"]).Returns("15");
        A.CallTo(() => vcSection["MaxAttempts"]).Returns("3");
        A.CallTo(() => _configuration.GetSection("VerificationCode")).Returns(vcSection);

        _jwtHelper = new JwtHelper(_configuration);
        _refreshTokenHelper = new RefreshTokenHelper(_configuration);

        _authService = new AuthService(
            _currentUserService,
            _userRepo,
            _roleRepo,
            _refreshTokenRepo,
            _jwtHelper,
            _refreshTokenHelper,
            _projectRepository,
            _verificationCodeRepository,
            _resend,
            _httpContextAccessor,
            _configuration
        );
    }

    private User CreateTestUser(string username = "testuser", string email = "test@example.com", string puid = "puid1", int roleId = 1, string password = "password")
    {
        return new User
        {
            User_Id = 1,
            Username = username,
            Puid = puid,
            Role_Id = roleId,
            Email = email,
            Password_Hash = PasswordHelper.HashPassword(password),
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    private Project CreateTestProject(string puid = "project1234", int userId = 1, bool isPublic = false)
    {
        return new Project
        {
            Project_Id = 1,
            User_Id = userId,
            Name = "Test Project",
            Puid = puid,
            Is_Public = isPublic,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsSuccessAndTokens()
    {
        // Arrange
        var username = "user1";
        var password = "password";
        var passwordHash = PasswordHelper.HashPassword(password);
        var user = CreateTestUser(username, "user1@example.com", "puid1", 1, password);
        var role = new Role { Role_Id = 1, Role_Name = "User" };

        A.CallTo(() => _userRepo.GetByUsername(username)).Returns(user);
        A.CallTo(() => _userRepo.GetPasswordHash(user.User_Id)).Returns(passwordHash);
        A.CallTo(() => _roleRepo.GetRole(user.Role_Id)).Returns(role);
        A.CallTo(() => _refreshTokenRepo.GetRefreshTokensByUserId(user.User_Id)).Returns(new List<RefreshToken>());

        // Act
        var (result, accessToken, refreshToken) = await _authService.Login(username, password);

        // Assert
        Assert.Equal(ServiceStatus.Ok200, result.Status);
        Assert.NotNull(accessToken);
        Assert.NotNull(refreshToken);
        Assert.Equal("puid1", result.Data?.Puid);
    }

    [Fact]
    public async Task Login_InvalidUsername_ReturnsUnauthorized()
    {
        // Arrange
        A.CallTo(() => _userRepo.GetByUsername("nonexistent")).Returns((User?)null);

        // Act
        var (result, accessToken, refreshToken) = await _authService.Login("nonexistent", "password");

        // Assert
        Assert.Equal(ServiceStatus.Unauthorized401, result.Status);
        Assert.Null(accessToken);
        Assert.Null(refreshToken);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var username = "user1";
        var passwordHash = PasswordHelper.HashPassword("correct");
        var user = CreateTestUser(username, "user1@example.com", "puid1", 1, "correct");

        A.CallTo(() => _userRepo.GetByUsername(username)).Returns(user);
        A.CallTo(() => _userRepo.GetPasswordHash(user.User_Id)).Returns(passwordHash);

        // Act
        var (result, accessToken, refreshToken) = await _authService.Login(username, "wrong");

        // Assert
        Assert.Equal(ServiceStatus.Unauthorized401, result.Status);
    }

    [Fact]
    public async Task Login_LockedOutUser_ReturnsUnauthorized()
    {
        // Arrange
        var user = CreateTestUser("user1", "user1@example.com", "puid1", 1, "password");
        user.Lockout_Until = DateTime.UtcNow.AddMinutes(10); // Locked out for 10 minutes
        A.CallTo(() => _userRepo.GetByUsername("user1")).Returns(user);

        // Act
        var (result, accessToken, refreshToken) = await _authService.Login("user1", "password");

        // Assert
        Assert.Equal(ServiceStatus.Unauthorized401, result.Status);
    }

    [Fact]
    public async Task Login_UserRoleNotFound_ReturnsInternalServerError()
    {
        // Arrange
        var username = "user1";
        var password = "password";
        var passwordHash = PasswordHelper.HashPassword(password);
        var user = CreateTestUser(username, "user1@example.com", "puid1", 99, password);

        A.CallTo(() => _userRepo.GetByUsername(username)).Returns(user);
        A.CallTo(() => _userRepo.GetPasswordHash(user.User_Id)).Returns(passwordHash);
        A.CallTo(() => _roleRepo.GetRole(user.Role_Id)).Returns((Role?)null);

        // Act
        var (result, accessToken, refreshToken) = await _authService.Login(username, password);

        // Assert
        Assert.Equal(ServiceStatus.InternalServerError500, result.Status);
    }

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsSuccessAndNewAccessToken()
    {
        // Arrange
        var tokenString = "valid-token";
        var storedToken = new RefreshToken 
        { 
            Token = tokenString,
            Token_Id = Guid.NewGuid(),
            User_Id = 1, 
            Expires_At = DateTime.UtcNow.AddDays(1),
            Created_At = DateTime.UtcNow
        };
        var user = CreateTestUser(roleId: 1);
        var role = new Role { Role_Id = 1, Role_Name = "User" };

        A.CallTo(() => _refreshTokenRepo.GetRefreshTokenByToken(tokenString)).Returns(storedToken);
        A.CallTo(() => _userRepo.GetById(1)).Returns(user);
        A.CallTo(() => _roleRepo.GetRole(1)).Returns(role);

        // Act
        var (result, accessToken) = await _authService.RefreshToken(tokenString);

        // Assert
        Assert.Equal(ServiceStatus.Ok200, result.Status);
        Assert.NotNull(accessToken);
    }

    [Fact]
    public async Task RefreshToken_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        A.CallTo(() => _refreshTokenRepo.GetRefreshTokenByToken("invalid")).Returns((RefreshToken?)null);

        // Act
        var (result, accessToken) = await _authService.RefreshToken("invalid");

        // Assert
        Assert.Equal(ServiceStatus.Unauthorized401, result.Status);
    }

    [Fact]
    public async Task RefreshToken_ExpiredToken_ReturnsUnauthorized()
    {
        // Arrange
        var storedToken = new RefreshToken 
        { 
            Token = "expired",
            Token_Id = Guid.NewGuid(),
            User_Id = 1,
            Expires_At = DateTime.UtcNow.AddDays(-1),
            Created_At = DateTime.UtcNow.AddDays(-2)
        };
        A.CallTo(() => _refreshTokenRepo.GetRefreshTokenByToken("expired")).Returns(storedToken);

        // Act
        var (result, accessToken) = await _authService.RefreshToken("expired");

        // Assert
        Assert.Equal(ServiceStatus.Unauthorized401, result.Status);
    }

    [Fact]
    public async Task RefreshToken_UserNotFound_ReturnsUnauthorized()
    {
        // Arrange
        var storedToken = new RefreshToken 
        { 
            Token = "token",
            Token_Id = Guid.NewGuid(),
            User_Id = 1, 
            Expires_At = DateTime.UtcNow.AddDays(1),
            Created_At = DateTime.UtcNow
        };
        A.CallTo(() => _refreshTokenRepo.GetRefreshTokenByToken("token")).Returns(storedToken);
        A.CallTo(() => _userRepo.GetById(1)).Returns((User?)null);

        // Act
        var (result, accessToken) = await _authService.RefreshToken("token");

        // Assert
        Assert.Equal(ServiceStatus.Unauthorized401, result.Status);
    }

    [Fact]
    public async Task RefreshToken_RoleNotFound_ReturnsInternalServerError()
    {
        // Arrange
        var storedToken = new RefreshToken 
        { 
            Token = "token",
            Token_Id = Guid.NewGuid(),
            User_Id = 1, 
            Expires_At = DateTime.UtcNow.AddDays(1),
            Created_At = DateTime.UtcNow
        };
        var user = CreateTestUser();

        A.CallTo(() => _refreshTokenRepo.GetRefreshTokenByToken("token")).Returns(storedToken);
        A.CallTo(() => _userRepo.GetById(1)).Returns(user);
        A.CallTo(() => _roleRepo.GetRole(1)).Returns((Role?)null);

        // Act
        var (result, accessToken) = await _authService.RefreshToken("token");

        // Assert
        Assert.Equal(ServiceStatus.InternalServerError500, result.Status);
    }

    [Fact]
    public async Task IsPublic_ProjectIsPublic_ReturnsTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var routeData = new RouteData();
        routeData.Values["projectPuid"] = "project1234";
        context.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = routeData });
        A.CallTo(() => _httpContextAccessor.HttpContext).Returns(context);
        A.CallTo(() => _projectRepository.GetProjectByPuid("project1234")).Returns(CreateTestProject(puid: "project1234", isPublic: true));

        // Act
        var result = await _authService.IsPublic();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsPublic_ProjectIsNotPublic_ReturnsFalse()
    {
        // Arrange
        var projectPuid = "project-puid";
        var context = new DefaultHttpContext();
        var routeData = new RouteData();
        routeData.Values["projectPuid"] = projectPuid;
        context.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = routeData });
        A.CallTo(() => _httpContextAccessor.HttpContext).Returns(context);
        A.CallTo(() => _projectRepository.GetProjectByPuid(projectPuid)).Returns(CreateTestProject(puid: projectPuid, isPublic: false));

        // Act
        var result = await _authService.IsPublic();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsOwner_UserIsOwner_ReturnsTrue()
    {
        // Arrange
        var userPuid = "user-puid";
        var projectPuid = "project-puid";
        var user = CreateTestUser(puid: userPuid, roleId: 1);
        var project = CreateTestProject(puid: projectPuid, userId: user.User_Id);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userPuid) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var context = new DefaultHttpContext();
        var routeData = new RouteData();
        routeData.Values["projectPuid"] = projectPuid;
        context.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = routeData });
        A.CallTo(() => _httpContextAccessor.HttpContext).Returns(context);
        A.CallTo(() => _userRepo.GetByPuid(userPuid)).Returns(user);
        A.CallTo(() => _projectRepository.GetProjectByPuid(projectPuid)).Returns(project);

        // Act
        var result = await _authService.IsOwner(principal);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsOwner_UserIsNotOwner_ReturnsFalse()
    {
        // Arrange
        var userPuid = "user-puid";
        var projectPuid = "project-puid";
        var user = CreateTestUser(puid: userPuid, roleId: 1);
        var project = CreateTestProject(puid: projectPuid, userId: 2); // Different owner
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userPuid) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var context = new DefaultHttpContext();
        var routeData = new RouteData();
        routeData.Values["projectPuid"] = projectPuid;
        context.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = routeData });
        A.CallTo(() => _httpContextAccessor.HttpContext).Returns(context);
        A.CallTo(() => _userRepo.GetByPuid(userPuid)).Returns(user);
        A.CallTo(() => _projectRepository.GetProjectByPuid(projectPuid)).Returns(project);

        // Act
        var result = await _authService.IsOwner(principal);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAdmin_UserIsAdmin_ReturnsTrue()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.Role, "Admin") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act
        var result = _authService.IsAdmin(principal);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAdmin_UserIsNotAdmin_ReturnsFalse()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.Role, "User") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act
        var result = _authService.IsAdmin(principal);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RequestVerificationCode_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var userPuid = "user-puid";
        var user = CreateTestUser(puid: userPuid, roleId: 2, email: "test@example.com");
        var role = new Role { Role_Id = 2, Role_Name = "Unverified" };

        A.CallTo(() => _currentUserService.UserPuid).Returns(userPuid);
        A.CallTo(() => _userRepo.GetByPuid(userPuid)).Returns(user);
        A.CallTo(() => _roleRepo.GetRole(user.Role_Id)).Returns(role);
        A.CallTo(() => _verificationCodeRepository.GetVerificationCodesByUserId(1)).Returns(new List<VerificationCode>());

        // Act
        var result = await _authService.RequestVerificationCode();

        // Assert
        Assert.Equal(ServiceStatus.Ok200, result.Status);
        A.CallTo(() => _verificationCodeRepository.AddVerificationCode(A<VerificationCode>._)).MustHaveHappened();
        A.CallTo(_resend).Where(x => x.Method.Name == "EmailSendAsync").MustHaveHappened();
    }

    [Fact]
    public async Task RequestVerificationCode_TooManyRequests_ReturnsError()
    {
        // Arrange
        var userPuid = "user-puid";
        var user = CreateTestUser(puid: userPuid, roleId: 2);
        var role = new Role { Role_Id = 2, Role_Name = "Unverified" };
        var codes = new List<VerificationCode> 
        { 
            new() { Code_Id = Guid.NewGuid(), User_Id = 1, Code_Hash = "h1", Created_At = DateTime.UtcNow, Expires_At = DateTime.UtcNow.AddMinutes(10), Attempts = 0 }, 
            new() { Code_Id = Guid.NewGuid(), User_Id = 1, Code_Hash = "h2", Created_At = DateTime.UtcNow, Expires_At = DateTime.UtcNow.AddMinutes(10), Attempts = 0 }, 
            new() { Code_Id = Guid.NewGuid(), User_Id = 1, Code_Hash = "h3", Created_At = DateTime.UtcNow, Expires_At = DateTime.UtcNow.AddMinutes(10), Attempts = 0 } 
        }; // Max is 3

        A.CallTo(() => _currentUserService.UserPuid).Returns(userPuid);
        A.CallTo(() => _userRepo.GetByPuid(userPuid)).Returns(user);
        A.CallTo(() => _roleRepo.GetRole(user.Role_Id)).Returns(role);
        A.CallTo(() => _verificationCodeRepository.GetVerificationCodesByUserId(1)).Returns(codes);

        // Act
        var result = await _authService.RequestVerificationCode();

        // Assert
        Assert.Equal(ServiceStatus.TooManyRequests429, result.Status);
    }

    [Fact]
    public async Task VerifyCode_ValidCode_ReturnsSuccess()
    {
        // Arrange
        var userPuid = "user-puid";
        var user = CreateTestUser(puid: userPuid, roleId: 2);
        var (code, hash) = VerificationCodeHelper.GenerateCode();
        var vc = new VerificationCode { Code_Id = Guid.NewGuid(), User_Id = 1, Code_Hash = hash, Created_At = DateTime.UtcNow, Expires_At = DateTime.UtcNow.AddMinutes(10), Attempts = 0 };

        A.CallTo(() => _currentUserService.UserPuid).Returns(userPuid);
        A.CallTo(() => _userRepo.GetByPuid(userPuid)).Returns(user);
        A.CallTo(() => _verificationCodeRepository.GetVerificationCodesByUserId(1)).Returns(new List<VerificationCode> { vc });
        A.CallTo(() => _roleRepo.GetRole("User")).Returns(new Role { Role_Id = 1, Role_Name = "User" });
        A.CallTo(() => _userRepo.GetById(1)).Returns(user);

        // Act
        var result = await _authService.VerifyCode(code);

        // Assert
        Assert.Equal(ServiceStatus.Ok200, result.Status);
        A.CallTo(() => _userRepo.UpdateUser(A<User>.That.Matches(u => u.Role_Id == 1))).MustHaveHappened();
    }

    [Fact]
    public async Task VerifyCode_InvalidCode_ReturnsError()
    {
        // Arrange
        var userPuid = "user-puid";
        var user = CreateTestUser(puid: userPuid, roleId: 2);
        var (_, hash) = VerificationCodeHelper.GenerateCode();
        var vc = new VerificationCode { Code_Id = Guid.NewGuid(), User_Id = 1, Code_Hash = hash, Created_At = DateTime.UtcNow, Expires_At = DateTime.UtcNow.AddMinutes(10), Attempts = 0 };

        A.CallTo(() => _currentUserService.UserPuid).Returns(userPuid);
        A.CallTo(() => _userRepo.GetByPuid(userPuid)).Returns(user);
        A.CallTo(() => _verificationCodeRepository.GetVerificationCodesByUserId(1)).Returns(new List<VerificationCode> { vc });

        // Act
        var result = await _authService.VerifyCode("wrong-code");

        // Assert
        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }
}
