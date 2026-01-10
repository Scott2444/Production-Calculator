using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Models;
using System.Security.Claims;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IAuthService
    {
        Task<(ServiceResult<AuthResponse> result, string? token)> Login(string username, string password);
        Task<(ServiceResult<AuthResponse> result, string? token)> RefreshToken(ClaimsPrincipal token);
        Task<bool> IsOwner(ClaimsPrincipal user, string? route);
        Task<bool> IsAdmin(ClaimsPrincipal user);
    }
}
