using System.Threading.Tasks;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetById(int id);
        Task<User?> GetByPuid(string puid);
        Task<User?> GetByUsername(string username);
        Task<User?> GetByEmail(string email);
        Task AddUser(User user);
        Task<bool> DeleteUser(int id);
        Task<string> GetPasswordHash(int id);
        Task<bool> PuidExists(string puid);
    }
}
