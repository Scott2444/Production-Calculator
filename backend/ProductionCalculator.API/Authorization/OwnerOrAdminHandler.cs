using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ProductionCalculator.API.Authorization
{
    public class OwnerOrAdminHandler : AuthorizationHandler<OwnerOrAdminRequirement>
    {
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, OwnerOrAdminRequirement requirement)
        {
            // Must be authenticated
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                context.Fail();
                return;
            }

            var roleName = context.User.FindFirst(ClaimTypes.Role)?.Value;
            // Check if user is admin
            if (roleName != null && roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                context.Succeed(requirement);
                return;
            }

            var httpContext = context.Resource as HttpContext;
            if (httpContext == null)
            {
                context.Fail();
                return;
            }

            // Try to get pubId from route
            string? routePubId = null;
            if (httpContext.Request.RouteValues.TryGetValue("pubId", out var pubIdObj))
            {
                routePubId = pubIdObj?.ToString();
            }

            if (string.IsNullOrEmpty(routePubId))
            {
                context.Fail();
                return;
            }

            // Get the claim from the JWT
            var claimUserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // If accessing user resource, compare directly
            if (httpContext.Request.Path.Value?.Contains("/users/") == true)
            {
                if (claimUserId != null && routePubId == claimUserId)
                {
                    context.Succeed(requirement);
                    return;
                }
                else
                {
                    context.Fail();
                    return;
                }
            }

            // For projects or workflows, fetch resource and compare owner
            // var serviceProvider = httpContext.RequestServices;
            // if (httpContext.Request.Path.Value?.Contains("/projects/") == true)
            // {
            //     var projectRepo = serviceProvider.GetService(typeof(Business.Interfaces.IProjectRepository)) as Business.Interfaces.IProjectRepository;
            //     if (projectRepo != null)
            //     {
            //         var project = await projectRepo.GetByPuid(routePubId!);
            //         if (project != null && claimUserId != null && project.OwnerPuid == claimUserId)
            //         {
            //             context.Succeed(requirement);
            //             return;
            //         }
            //     }
            //     context.Fail();
            //     return;
            // }
            // else if (httpContext.Request.Path.Value?.Contains("/workflows/") == true)
            // {
            //     var workflowRepo = serviceProvider.GetService(typeof(Business.Interfaces.IWorkflowRepository)) as Business.Interfaces.IWorkflowRepository;
            //     if (workflowRepo != null)
            //     {
            //         var workflow = await workflowRepo.GetByPuid(routePubId!);
            //         if (workflow != null && claimUserId != null && workflow.OwnerPuid == claimUserId)
            //         {
            //             context.Succeed(requirement);
            //             return;
            //         }
            //     }
            //     context.Fail();
            //     return;
            // }

            // Default: fail
            context.Fail();
        }
    }
}