using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Models;
using System.Security.Claims;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IAuthService
    {
        Task<(ServiceResult<AuthResponse> result, string? accessToken, RefreshToken? refreshToken)> Login(string username, string password);
        Task<(ServiceResult<AuthResponse> result, string? accessToken)> RefreshToken(string? refreshToken);
        Task<bool> IsOwner(ClaimsPrincipal user, string? route);
        Task<bool> IsAdmin(ClaimsPrincipal user);
    }
}
