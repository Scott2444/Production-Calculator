using Microsoft.AspNetCore.Authorization;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.API.Authorization
{
    public class PublicHandler : AuthorizationHandler<PublicRequirement>
    {
        private readonly IServiceProvider _serviceProvider;

        public PublicHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PublicRequirement requirement)
        {
            var httpContext = context.Resource as HttpContext;
            if (httpContext == null) { context.Fail(); return; }
            
            // Resolve IAuthService from the request scope
            var authService = httpContext.RequestServices.GetService(typeof(IAuthService)) as IAuthService;
            if (authService == null) { context.Fail(); return; }

            var IsPublic = await authService.IsPublic();
            var isOwner = await authService.IsOwner(context.User);
            var isAdmin = authService.IsAdmin(context.User);

            if (IsPublic || isOwner || isAdmin)
                context.Succeed(requirement);
            else
                context.Fail();
        }
    }
}