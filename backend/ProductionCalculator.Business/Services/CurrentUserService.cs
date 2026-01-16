using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Business.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        public string? UserPuid { get; }

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            var user = httpContextAccessor.HttpContext?.User;
            UserPuid = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}