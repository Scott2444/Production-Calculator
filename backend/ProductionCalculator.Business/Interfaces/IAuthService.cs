using ProductionCalculator.Business.Models;
using System.Security.Claims;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IAuthService
    {
        Task<ServiceResult<string>> Login(string username, string password);
        Task<ServiceResult<string>> RefreshToken(ClaimsPrincipal token);
    }
}
