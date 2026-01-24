using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IWorkflowService
    {
        Task<ServiceResult<Workflow>> AddWorkflow(string projectPuid, string name, string? description);
        Task<ServiceResult<Workflow>> UpdateWorkflow(string projectPuid, string puid, string? name, string? description);
        Task<ServiceResult<Workflow>> GetWorkflowByPuid(string projectPuid, string puid);
        Task<ServiceResult<List<Workflow>>> GetWorkflowsByProjectPuid(string projectPuid);
        Task<ServiceResult> DeleteWorkflow(string projectPuid, string puid);
    }
}