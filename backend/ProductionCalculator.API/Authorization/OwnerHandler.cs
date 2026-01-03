using Microsoft.AspNetCore.Authorization;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.API.Authorization
{
    public class OwnerHandler : AuthorizationHandler<OwnerRequirement>
    {
        private readonly IServiceProvider _serviceProvider;

        public OwnerHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, OwnerRequirement requirement)
        {
            var httpContext = context.Resource as HttpContext;
            if (httpContext == null) { context.Fail(); return; }

            var route = httpContext.Request.Path.Value;
            if (string.IsNullOrEmpty(route)) { context.Fail(); return; }
            
            // Resolve IAuthService from the request scope
            var authService = httpContext.RequestServices.GetService(typeof(IAuthService)) as IAuthService;
            if (authService == null) { context.Fail(); return; }

            var isOwner = await authService.IsOwner(context.User, route: route);

            if (isOwner)
                context.Succeed(requirement);
            else
                context.Fail();
        }
    }
}