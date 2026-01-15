using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IMachineRepository
    {
        Task AddMachine(Machine machine);
        Task<Machine?> GetMachineById(int id);
        Task<Machine?> GetMachineByPuid(string puid);
        Task<List<Machine>> GetMachinesByProjectId(int projectId);
        Task<Machine> UpdateMachine(Machine machine);
        Task<bool> DeleteMachine(int id);
        Task<bool> PuidExists(string puid);
    }
}