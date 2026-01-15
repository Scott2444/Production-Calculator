using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IMachineRecipeRepository
    {
        Task<MachineRecipe?> GetById(int id);
        Task<IEnumerable<MachineRecipe>> GetByMachineId(int machineId);
        Task AddMachineRecipes(IEnumerable<MachineRecipe> machineRecipes);
        Task<bool> DeleteMachineRecipe(int id);
        Task<List<bool>> DeleteMachineRecipes(IEnumerable<int> ids);
    }
}
