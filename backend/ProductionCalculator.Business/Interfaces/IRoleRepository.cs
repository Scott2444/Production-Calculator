using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IRoleRepository
    {
        Task<Role?> GetRole(int id);
        Task<Role?> GetRole(string roleName);
    }
}
