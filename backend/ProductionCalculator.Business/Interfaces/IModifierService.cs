using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IModifierService
    {
        Task<ServiceResult<Modifier>> AddModifier(string projectPuid, string name, string? description, double flatBonus, double percentBonus, double multiplicativeBonus, double inputMultiplier = 1.0, double outputMultiplier = 1.0, List<ModifierAttributeExchange>? attributes = null);
        Task<ServiceResult<Modifier>> UpdateModifier(string projectPuid, string puid, string? name, string? description, double flatBonus, double percentBonus, double multiplicativeBonus, double inputMultiplier = 1.0, double outputMultiplier = 1.0, List<ModifierAttributeExchange>? attributes = null);
        Task<ServiceResult<Modifier>> GetModifierByPuid(string projectPuid, string puid);
        Task<ServiceResult<List<Modifier>>> GetModifiersByProjectPuid(string projectPuid);
        Task<ServiceResult> DeleteModifier(string projectPuid, string puid);
    }
}