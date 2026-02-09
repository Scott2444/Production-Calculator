using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IWorkflowService
    {
        // Workflow object
        Task<ServiceResult<Workflow>> AddWorkflow(string projectPuid, string name, string? description);
        Task<ServiceResult<Workflow>> UpdateWorkflow(string projectPuid, string puid, string? name, string? description);
        Task<ServiceResult<Workflow>> GetWorkflowByPuid(string projectPuid, string puid);
        Task<ServiceResult<List<Workflow>>> GetWorkflowsByProjectPuid(string projectPuid);
        Task<ServiceResult> DeleteWorkflow(string projectPuid, string puid);

        // Workflow chart
        Task<ServiceResult<WorkflowChartResponse>> GetWorkflowChartById(string projectPuid, string workflowPuid);
        Task<ServiceResult<WorkflowChartResponse>> UpdateTargetDemand(string projectPuid, string workflowPuid, List<(string productPuid, double rate)> rootDemands);
        Task<ServiceResult<WorkflowChartResponse>> UpdateNode(string projectPuid, string workflowPuid, string nodePuid, WorkflowNodeRequest request);
        Task<ServiceResult<WorkflowChartResponse>> SetRecipes(string projectPuid, string workflowPuid, List<string> recipePuids);
        Task<ServiceResult<WorkflowChartResponse>> SetExternal(string projectPuid, string workflowPuid, string productPuid, bool isExternal, double? externalRate);
        Task<ServiceResult<WorkflowChartResponse>> UpgradeWorkflowChart(string projectPuid, string workflowPuid);
    }
}