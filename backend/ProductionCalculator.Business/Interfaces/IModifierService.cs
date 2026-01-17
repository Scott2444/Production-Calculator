using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IModifierService
    {
        Task<ServiceResult<Modifier>> AddModifier(string projectPuid, string name, string? description, double flat_speed_bonus, double additive_percent_bonus, double multiplicative_modifiers);
        Task<ServiceResult<Modifier>> UpdateModifier(string projectPuid, string puid, string? name, string? description, double flat_speed_bonus, double additive_percent_bonus, double multiplicative_modifiers);
        Task<ServiceResult<Modifier>> GetModifierByPuid(string projectPuid, string puid);
        Task<ServiceResult<List<Modifier>>> GetModifiersByProjectPuid(string projectPuid);
        Task<ServiceResult> DeleteModifier(string projectPuid, string puid);
    }
}