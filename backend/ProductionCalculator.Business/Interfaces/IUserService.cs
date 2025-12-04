using System.Threading.Tasks;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IUserService
    {
        Task<ServiceResult<User>> Register(string username, string email, string password);
        Task<ServiceResult<User>> GetUserByPubId(string pubId);
        Task<ServiceResult<User>> GetUserByUsername(string username);
        Task<ServiceResult> DeleteUserById(string pubId);
    }
}
