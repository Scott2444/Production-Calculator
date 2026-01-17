using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IModifierRepository
    {
        Task AddModifier(Modifier modifier);
        Task<Modifier?> GetModifierById(int id);
        Task<Modifier?> GetModifierByPuid(string puid);
        Task<List<Modifier>> GetModifiersByProjectId(int projectId);
        Task<Modifier> UpdateModifier(Modifier modifier);
        Task<bool> DeleteModifier(int id);
        Task<bool> PuidExists(string puid);
    }
}