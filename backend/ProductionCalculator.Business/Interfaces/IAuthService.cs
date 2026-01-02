using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Models;
using System.Security.Claims;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IAuthService
    {
        Task<ServiceResult<AuthResponse>> Login(string username, string password);
        Task<ServiceResult<AuthResponse>> RefreshToken(ClaimsPrincipal token);
        Task<bool> IsOwner(ClaimsPrincipal user, string? pubId, string? route);
        Task<bool> IsAdmin(ClaimsPrincipal user);
    }
}
