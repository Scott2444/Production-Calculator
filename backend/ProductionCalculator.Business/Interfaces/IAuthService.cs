using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Models;
using System.Security.Claims;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IAuthService
    {
        Task<(ServiceResult<AuthResponse> result, string? accessToken, RefreshToken? refreshToken)> Login(string username, string password, bool generateToken = true);
        Task<(ServiceResult<AuthResponse> result, string? accessToken)> RefreshToken(string? refreshToken);
        Task<ServiceResult> RequestPasswordReset(string email);
        Task<ServiceResult> ResetPassword(string token, string newPassword);
        Task<ServiceResult> RequestVerificationCode();
        Task<ServiceResult> VerifyCode(string code);
        Task<bool> IsPublic();
        Task<bool> IsOwner(ClaimsPrincipal user);
        bool IsAdmin(ClaimsPrincipal user);
    }
}
