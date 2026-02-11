using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.APIModels;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IWorkflowChartService
    {
        Task<ServiceResult<WorkflowChartResponse>> GetWorkflowChartById(Workflow workflow);
        Task<ServiceResult<WorkflowChartResponse>> UpsertRootDemands(Workflow workflow, List<(string productPuid, double rate)> rootDemands);
        Task<ServiceResult<WorkflowChartResponse>> UpdateNode(Workflow workflow, string nodePuid, WorkflowNodeRequest request);
        Task<ServiceResult<WorkflowChartResponse>> SetRecipes(Workflow workflow, List<string> recipePuids);
        Task<ServiceResult<WorkflowChartResponse>> SetExternal(Workflow workflow, string productPuid, bool isExternal, double? externalRate);
        Task<ServiceResult<WorkflowChartResponse>> UpgradeWorkflowChart(Workflow workflow);
    }
}
