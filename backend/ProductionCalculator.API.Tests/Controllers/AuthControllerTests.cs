using System.Diagnostics.CodeAnalysis;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.API.Controllers;
using ProductionCalculator.API.Helpers;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using Xunit;
using Microsoft.Extensions.Configuration;

namespace ProductionCalculator.API.Tests.Controllers;

[ExcludeFromCodeCoverage]
public class AuthControllerTests
{
    private readonly IAuthService _authService;
    private readonly CookieOptionsHelper _cookieOptionsHelper;
    private readonly AuthController _controller;
    private readonly DefaultHttpContext _httpContext;

    public AuthControllerTests()
    {
        _authService = A.Fake<IAuthService>();
        
        var config = A.Fake<IConfiguration>();
        A.CallTo(() => config["Jwt:ExpireMinutes"]).Returns("60");
        A.CallTo(() => config["RefreshToken:ExpireDays"]).Returns("7");

        _cookieOptionsHelper = new CookieOptionsHelper(config);
        _controller = new AuthController(_authService, _cookieOptionsHelper);
        
        _httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _httpContext
        };
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkAndSetsCookies()
    {
        // Arrange
        var req = new LoginRequest { Username = "user", Password = "password" };
        var authResponse = new AuthResponse { Puid = "puid1", Username = "user" };
        var refreshToken = new RefreshToken 
        { 
            Token = "rt-123", 
            Token_Id = Guid.NewGuid(), 
            User_Id = 1, 
            Expires_At = DateTime.UtcNow.AddDays(7), 
            Created_At = DateTime.UtcNow 
        };
        
        A.CallTo(() => _authService.Login(req.Username, req.Password, true))
            .Returns((ServiceResult<AuthResponse>.SuccessResult(authResponse), "at-123", refreshToken));

        // Act
        var result = await _controller.Login(req);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.Equal(authResponse, objectResult.Value);
        Assert.True(_httpContext.Response.Headers.ContainsKey("Set-Cookie"));
        var cookies = _httpContext.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("access_token=at-123", cookies);
        Assert.Contains("refresh_token=rt-123", cookies);
        Assert.Contains("user_id=puid1", cookies);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var req = new LoginRequest { Username = "user", Password = "wrong" };
        A.CallTo(() => _authService.Login(req.Username, req.Password, true))
            .Returns((ServiceResult<AuthResponse>.Fail(ServiceStatus.Unauthorized401, "Invalid"), null, null));

        // Act
        var result = await _controller.Login(req);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(401, objectResult.StatusCode);
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsOkAndSetsCookie()
    {
        // Arrange
        _httpContext.Request.Headers["Cookie"] = "refresh_token=rt-123";
        A.CallTo(() => _authService.RefreshToken("rt-123"))
            .Returns((ServiceResult<AuthResponse>.SuccessResult(new AuthResponse()), "new-at"));

        // Act
        var result = await _controller.Refresh();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        var cookies = _httpContext.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("access_token=new-at", cookies);
    }

    [Fact]
    public async Task Refresh_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        _httpContext.Request.Headers["Cookie"] = "refresh_token=invalid";
        A.CallTo(() => _authService.RefreshToken("invalid"))
            .Returns((ServiceResult<AuthResponse>.Fail(ServiceStatus.Unauthorized401, "Invalid"), null));

        // Act
        var result = await _controller.Refresh();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(401, objectResult.StatusCode);
    }

    [Fact]
    public async Task RequestVerificationCode_AuthenticatedUser_ReturnsOk()
    {
        // Arrange
        A.CallTo(() => _authService.RequestVerificationCode())
            .Returns(ServiceResult.SuccessResult());

        // Act
        var result = await _controller.RequestVerificationCode();

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(200, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task RequestPasswordReset_ValidRequest_ReturnsOk()
    {
        // Arrange
        A.CallTo(() => _authService.RequestPasswordReset("test@example.com"))
            .Returns(ServiceResult.SuccessResult());

        // Act
        var result = await _controller.RequestPasswordReset(new RequestPasswordResetRequest { Email = "test@example.com" });

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(200, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_ValidRequest_ReturnsOk()
    {
        // Arrange
        var req = new ResetPasswordRequest { Token = "token", NewPassword = "password123" };
        A.CallTo(() => _authService.ResetPassword(req.Token, req.NewPassword))
            .Returns(ServiceResult.SuccessResult());

        // Act
        var result = await _controller.ResetPassword(req);

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(200, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var req = new ResetPasswordRequest { Token = "bad", NewPassword = "password123" };
        A.CallTo(() => _authService.ResetPassword(req.Token, req.NewPassword))
            .Returns(ServiceResult.Fail(ServiceStatus.BadRequest400, "Invalid"));

        // Act
        var result = await _controller.ResetPassword(req);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    [Fact]
    public async Task VerifyCode_ValidCode_ReturnsOk()
    {
        // Arrange
        var req = new VerificationCodeRequest { Code = "123456" };
        A.CallTo(() => _authService.VerifyCode("123456"))
            .Returns(ServiceResult.SuccessResult());

        // Act
        var result = await _controller.VerifyCode(req);

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(200, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task VerifyCode_InvalidCode_ReturnsBadRequest()
    {
        // Arrange
        var req = new VerificationCodeRequest { Code = "wrong" };
        A.CallTo(() => _authService.VerifyCode("wrong"))
            .Returns(ServiceResult.Fail(ServiceStatus.BadRequest400, "Invalid"));

        // Act
        var result = await _controller.VerifyCode(req);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }
}
