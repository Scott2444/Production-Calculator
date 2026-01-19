using System.Threading.Tasks;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IUserService
    {
        Task<ServiceResult<User>> Register(string username, string email, string password);
        Task<ServiceResult> ValidateNewUser(string username, string email);
        Task<ServiceResult<(User, bool)>> GetUserByPuid(string puid);
        Task<ServiceResult<(User, bool)>> GetUserByUsername(string username);
        Task<ServiceResult> DeleteUserById(string puid);
    }
}
