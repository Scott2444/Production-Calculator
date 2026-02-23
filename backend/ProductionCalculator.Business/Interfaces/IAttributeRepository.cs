using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IAttributeRepository
    {
        Task AddAttribute(ProjectAttribute attribute);
        Task<ProjectAttribute?> GetAttributeById(int id);
        Task<ProjectAttribute?> GetAttributeByPuid(string puid);
        Task<List<ProjectAttribute>> GetAttributesByProjectId(int projectId);
        Task<ProjectAttribute> UpdateAttribute(ProjectAttribute attribute);
        Task<bool> DeleteAttribute(int id);
        Task<bool> PuidExists(string puid);
    }
}
