using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IModifierService
    {
        Task<ServiceResult<ModifierResponse>> AddModifier(string projectPuid, string name, string? description, double flatBonus, double percentBonus, double multiplicativeBonus, double inputPercent = 1.0, double outputPercent = 1.0, List<ModifierAttributeRequest>? attributes = null);
        Task<ServiceResult<ModifierResponse>> UpdateModifier(string projectPuid, string puid, string? name, string? description, double flatBonus, double percentBonus, double multiplicativeBonus, double inputPercent = 1.0, double outputPercent = 1.0, List<ModifierAttributeRequest>? attributes = null);
        Task<ServiceResult<ModifierResponse>> GetModifierByPuid(string projectPuid, string puid);
        Task<ServiceResult<List<ModifierResponse>>> GetModifiersByProjectPuid(string projectPuid);
        Task<ServiceResult> DeleteModifier(string projectPuid, string puid);
    }
}