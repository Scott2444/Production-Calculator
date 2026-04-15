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
        Task UpdateUser(User user);
        Task<bool> TryIncrementProjectCount(string puid, int maxAllowed);
        Task IncrementProjectCount(string puid);
        Task DecrementProjectCount(string puid);
        Task<bool> DeleteUser(int id);
        Task<string> GetPasswordHash(int id);
        Task<bool> PuidExists(string puid);
        Task<bool> IsRegistrationEnabled();
        Task SetRegistrationEnabled(bool isEnabled);
    }
}
