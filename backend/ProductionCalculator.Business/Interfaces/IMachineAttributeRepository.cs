using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IMachineAttributeRepository
    {
        Task<MachineAttribute?> GetById(int id);
        Task<IEnumerable<MachineAttribute>> GetByMachineId(int machineId);
        Task AddMachineAttributes(IEnumerable<MachineAttribute> machineAttributes);
        Task UpdateMachineAttributes(IEnumerable<MachineAttribute> machineAttributes);
        Task<bool> DeleteMachineAttribute(int id);
        Task<List<bool>> DeleteMachineAttributes(IEnumerable<int> ids);
    }
}
