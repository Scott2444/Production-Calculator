using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Business.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        public string? UserPuid { get; }
        public bool IsAuthenticated { get; }
        public bool IsAdmin { get; }

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            var user = httpContextAccessor.HttpContext?.User;
            IsAuthenticated = user?.Identity?.IsAuthenticated ?? false;
            UserPuid = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var roleName = user?.FindFirst(ClaimTypes.Role)?.Value;
            IsAdmin = roleName != null && roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }
    }
}