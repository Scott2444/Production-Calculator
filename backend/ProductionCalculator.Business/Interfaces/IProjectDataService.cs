using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IProjectDataService
    {
        Task<ProjectObjects> GetProjectObjects(int projectId);
    }
}