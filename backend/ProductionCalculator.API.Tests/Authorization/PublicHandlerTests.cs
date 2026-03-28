using System.Diagnostics.CodeAnalysis;
using FakeItEasy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using ProductionCalculator.API.Authorization;
using ProductionCalculator.Business.Interfaces;
using System.Security.Claims;

namespace ProductionCalculator.API.Tests;

[ExcludeFromCodeCoverage]
public class PublicHandlerTests
{
    private static ClaimsPrincipal CreateUser()
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "test-user")
        ], "TestAuth"));
    }

    private static AuthorizationHandlerContext CreateContext(object? resource = null)
    {
        var requirement = new PublicRequirement();
        return new AuthorizationHandlerContext(new[] { requirement }, CreateUser(), resource);
    }

    private static HttpContext CreateHttpContext(IAuthService? authService)
    {
        var serviceProvider = A.Fake<IServiceProvider>();
        A.CallTo(() => serviceProvider.GetService(typeof(IAuthService))).Returns(authService);

        return new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
    }

    [Fact]
    public async Task HandleAsync_HttpContextMissing_Fails()
    {
        var handler = new PublicHandler(A.Fake<IServiceProvider>());
        var context = CreateContext(resource: null);

        await handler.HandleAsync(context);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_AuthServiceMissing_Fails()
    {
        var handler = new PublicHandler(A.Fake<IServiceProvider>());
        var httpContext = CreateHttpContext(authService: null);
        var context = CreateContext(httpContext);

        await handler.HandleAsync(context);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_ProjectIsPublic_Succeeds()
    {
        var authService = A.Fake<IAuthService>();
        A.CallTo(() => authService.IsPublic()).Returns(true);
        A.CallTo(() => authService.IsOwner(A<ClaimsPrincipal>._)).Returns(false);
        A.CallTo(() => authService.IsAdmin(A<ClaimsPrincipal>._)).Returns(false);

        var handler = new PublicHandler(A.Fake<IServiceProvider>());
        var httpContext = CreateHttpContext(authService);
        var context = CreateContext(httpContext);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        Assert.False(context.HasFailed);
    }

    [Fact]
    public async Task HandleAsync_UserIsOwner_Succeeds()
    {
        var authService = A.Fake<IAuthService>();
        A.CallTo(() => authService.IsPublic()).Returns(false);
        A.CallTo(() => authService.IsOwner(A<ClaimsPrincipal>._)).Returns(true);
        A.CallTo(() => authService.IsAdmin(A<ClaimsPrincipal>._)).Returns(false);

        var handler = new PublicHandler(A.Fake<IServiceProvider>());
        var httpContext = CreateHttpContext(authService);
        var context = CreateContext(httpContext);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        Assert.False(context.HasFailed);
    }

    [Fact]
    public async Task HandleAsync_UserIsAdmin_Succeeds()
    {
        var authService = A.Fake<IAuthService>();
        A.CallTo(() => authService.IsPublic()).Returns(false);
        A.CallTo(() => authService.IsOwner(A<ClaimsPrincipal>._)).Returns(false);
        A.CallTo(() => authService.IsAdmin(A<ClaimsPrincipal>._)).Returns(true);

        var handler = new PublicHandler(A.Fake<IServiceProvider>());
        var httpContext = CreateHttpContext(authService);
        var context = CreateContext(httpContext);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        Assert.False(context.HasFailed);
    }

    [Fact]
    public async Task HandleAsync_ProjectNotPublicAndUserNotPrivileged_Fails()
    {
        var authService = A.Fake<IAuthService>();
        A.CallTo(() => authService.IsPublic()).Returns(false);
        A.CallTo(() => authService.IsOwner(A<ClaimsPrincipal>._)).Returns(false);
        A.CallTo(() => authService.IsAdmin(A<ClaimsPrincipal>._)).Returns(false);

        var handler = new PublicHandler(A.Fake<IServiceProvider>());
        var httpContext = CreateHttpContext(authService);
        var context = CreateContext(httpContext);

        await handler.HandleAsync(context);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }
}
