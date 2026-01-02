using Microsoft.AspNetCore.Authorization;
using ProductionCalculator.Business.Interfaces;
using System.Security.Claims;

namespace ProductionCalculator.API.Authorization
{
    public class UserHandler : AuthorizationHandler<UserRequirement>
    {
        private readonly IServiceProvider _serviceProvider;

        public UserHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, UserRequirement requirement)
        {
            var user = context.User;
            // Must be authenticated
            if (!user.Identity?.IsAuthenticated ?? true) { context.Fail(); return; }

            var roleName = user.FindFirst(ClaimTypes.Role)?.Value;
            // Check if user is role User or higher
            var isUser = roleName != null && 
                (roleName.Equals("User", StringComparison.OrdinalIgnoreCase) || 
                 roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase));
            if (!isUser)
            {
                context.Fail();
                return;
            }
            context.Succeed(requirement);
        }
    }
}