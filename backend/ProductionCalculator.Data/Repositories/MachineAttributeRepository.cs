using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Repositories
{
    public class MachineAttributeRepository : IMachineAttributeRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public MachineAttributeRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task<MachineAttribute?> GetById(int id)
        {
            return await _db.Set<MachineAttribute>().FindAsync(id);
        }

        public async Task<IEnumerable<MachineAttribute>> GetByMachineId(int machineId)
        {
            return await _db.Set<MachineAttribute>()
                .Where(ma => ma.Machine_Id == machineId)
                .ToListAsync();
        }

        public async Task AddMachineAttributes(IEnumerable<MachineAttribute> machineAttributes)
        {
            var machineAttributeList = machineAttributes.ToList();
            if (!machineAttributeList.Any()) return;

            await _db.Set<MachineAttribute>().AddRangeAsync(machineAttributeList);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateMachineAttributes(IEnumerable<MachineAttribute> machineAttributes)
        {
            var machineAttributeList = machineAttributes.ToList();
            if (!machineAttributeList.Any()) return;

            _db.Set<MachineAttribute>().UpdateRange(machineAttributeList);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> DeleteMachineAttribute(int id)
        {
            var machineAttribute = await _db.Set<MachineAttribute>().FindAsync(id);
            if (machineAttribute == null) return false;

            _db.Set<MachineAttribute>().Remove(machineAttribute);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<bool>> DeleteMachineAttributes(IEnumerable<int> ids)
        {
            var results = new List<bool>();
            foreach (var id in ids)
            {
                var result = await DeleteMachineAttribute(id);
                results.Add(result);
            }

            return results;
        }
    }
}
