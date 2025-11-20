using System.Threading.Tasks;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IAuthService
    {
        Task<ServiceResult<string>> Login(string username, string password);
        Task<ServiceResult<string>> RefreshToken(string token);
    }
}
