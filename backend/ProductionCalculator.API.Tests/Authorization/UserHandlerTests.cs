using System.Diagnostics.CodeAnalysis;
using FakeItEasy;
using Microsoft.AspNetCore.Authorization;
using ProductionCalculator.API.Authorization;
using System.Security.Claims;

namespace ProductionCalculator.API.Tests;

[ExcludeFromCodeCoverage]
public class UserHandlerTests
{
    private static ClaimsPrincipal CreateUser(string? role = null, bool isAuthenticated = true)
    {
        var claims = new List<Claim>();
        if (!string.IsNullOrWhiteSpace(role))
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = isAuthenticated
            ? new ClaimsIdentity(claims, "TestAuth")
            : new ClaimsIdentity(claims);

        return new ClaimsPrincipal(identity);
    }

    private static AuthorizationHandlerContext CreateContext(ClaimsPrincipal user)
    {
        var requirement = new UserRequirement();
        return new AuthorizationHandlerContext([requirement], user, resource: null);
    }

    [Fact]
    public async Task HandleAsync_UserNotAuthenticated_Fails()
    {
        var handler = new UserHandler(A.Fake<IServiceProvider>());
        var context = CreateContext(CreateUser(isAuthenticated: false));

        await handler.HandleAsync(context);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_AuthenticatedUserRole_Succeeds()
    {
        var handler = new UserHandler(A.Fake<IServiceProvider>());
        var context = CreateContext(CreateUser(role: "User"));

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        Assert.False(context.HasFailed);
    }

    [Fact]
    public async Task HandleAsync_AuthenticatedAdminRole_Succeeds()
    {
        var handler = new UserHandler(A.Fake<IServiceProvider>());
        var context = CreateContext(CreateUser(role: "Admin"));

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        Assert.False(context.HasFailed);
    }

    [Fact]
    public async Task HandleAsync_AuthenticatedNonUserRole_Fails()
    {
        var handler = new UserHandler(A.Fake<IServiceProvider>());
        var context = CreateContext(CreateUser(role: "Viewer"));

        await handler.HandleAsync(context);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_AuthenticatedWithoutRole_Fails()
    {
        var handler = new UserHandler(A.Fake<IServiceProvider>());
        var context = CreateContext(CreateUser());

        await handler.HandleAsync(context);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }
}
