using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ProductionCalculator.API.Authorization
{
    public class OwnerOrAdminHandler : AuthorizationHandler<OwnerOrAdminRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OwnerOrAdminRequirement requirement)
        {
        // Must be authenticated
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var roleName = context.User.FindFirst(ClaimTypes.Role)?.Value;

        // Check if user is admin
        if (roleName != null && roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Extract the {pubId} route value
        var pubIdFromRoute = context.Resource as HttpContext; // Resource is usually HttpContext when using policies on endpoints

        if (pubIdFromRoute == null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        if (!pubIdFromRoute.Request.RouteValues.TryGetValue("pubId", out var routePubIdObj))
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var routePubId = routePubIdObj?.ToString();
        if (string.IsNullOrEmpty(routePubId))
        {
            context.Fail();
            return Task.CompletedTask;
        }

        // Get the claim from the JWT
        var claimUserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (claimUserId != null && routePubId == claimUserId)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
        }
    }
}
