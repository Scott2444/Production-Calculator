using Microsoft.AspNetCore.Authorization;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.API.Authorization
{
    public class OwnerOrAdminHandler : AuthorizationHandler<OwnerOrAdminRequirement>
    {
        private readonly IServiceProvider _serviceProvider;

        public OwnerOrAdminHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, OwnerOrAdminRequirement requirement)
        {
            var httpContext = context.Resource as HttpContext;
            if (httpContext == null) { context.Fail(); return; }

            // Try to get puid from route
            string? routePuid = null;
            if (httpContext.Request.RouteValues.TryGetValue("puid", out var puidObj))
                routePuid = puidObj?.ToString();
            if (string.IsNullOrEmpty(routePuid)) { context.Fail(); return; }
            var route = httpContext.Request.Path.Value;
            if (string.IsNullOrEmpty(route)) { context.Fail(); return; }

            // Resolve IAuthService from the request scope
            var authService = httpContext.RequestServices.GetService(typeof(IAuthService)) as IAuthService;
            if (authService == null) { context.Fail(); return; }

            var isOwner = await authService.IsOwner(context.User, routePuid: routePuid, route: route);
            var isAdmin = await authService.IsAdmin(context.User);

            if (isOwner || isAdmin)
                context.Succeed(requirement);
            else
                context.Fail();
        }
    }
}