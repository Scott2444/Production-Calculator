using System.Threading.Tasks;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IUserService
    {
        Task<ServiceResult<User>> RegisterAsync(string username, string email, string password);
        Task<ServiceResult<User>> GetUserById(int id);
        Task<ServiceResult<User>> GetUserByUsername(string username);
        Task<ServiceResult> DeleteUserByUsername(string username);
    }
}
