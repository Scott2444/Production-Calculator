using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Business.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        public int? UserId { get; }
        public string? UserPuid { get; }

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            var user = httpContextAccessor.HttpContext?.User;
            UserId = user?.FindFirst(ClaimTypes.Name)?.Value != null ? int.Parse(user.FindFirst(ClaimTypes.Name)?.Value!) : null;
            UserPuid = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}