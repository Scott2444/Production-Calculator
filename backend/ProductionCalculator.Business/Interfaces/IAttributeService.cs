using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IAttributeService
    {
        Task<ServiceResult<ProjectAttribute>> AddAttribute(string projectPuid, string name, string? description, string? unit);
        Task<ServiceResult<ProjectAttribute>> GetAttributeByPuid(string projectPuid, string puid);
        Task<ServiceResult<List<ProjectAttribute>>> GetAttributesByProjectPuid(string projectPuid);
        Task<ServiceResult<ProjectAttribute>> UpdateAttribute(string projectPuid, string puid, string? name, string? description, string? unit);
        Task<ServiceResult> DeleteAttribute(string projectPuid, string puid);
    }
}
