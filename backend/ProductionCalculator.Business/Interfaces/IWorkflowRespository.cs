using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IWorkflowRepository
    {
        Task AddWorkflow(Workflow workflow);
        Task<Workflow?> GetWorkflowById(int id);
        Task<Workflow?> GetWorkflowByPuid(string puid);
        Task<List<Workflow>> GetWorkflowsByProjectId(int projectId);
        Task<Workflow> UpdateWorkflow(Workflow workflow);
        Task<bool> DeleteWorkflow(int id);
        Task<bool> PuidExists(string puid);
    }
}