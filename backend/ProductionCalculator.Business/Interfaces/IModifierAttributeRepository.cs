using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IModifierAttributeRepository
    {
        Task<ModifierAttribute?> GetById(int id);
        Task<IEnumerable<ModifierAttribute>> GetByModifierId(int modifierId);
        Task AddModifierAttributes(IEnumerable<ModifierAttribute> modifierAttributes);
        Task UpdateModifierAttributes(IEnumerable<ModifierAttribute> modifierAttributes);
        Task<bool> DeleteModifierAttribute(int id);
        Task<List<bool>> DeleteModifierAttributes(IEnumerable<int> ids);
    }
}
