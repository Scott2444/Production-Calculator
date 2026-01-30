using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.APIModels;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IWorkflowNodeService
    {
        Task<WorkflowChartResponse> GetWorkflowChartById(Workflow workflow);
        Task<WorkflowChartResponse> UpsertRootDemands(Workflow workflow, List<(string productPuid, double rate)> rootDemands);
        Task<WorkflowChartResponse> SetMachine(Workflow workflow, string nodePuid, string machinePuid);
        Task<WorkflowChartResponse> SetRecipe(Workflow workflow, string nodePuid, string? recipePuid);
        Task<WorkflowChartResponse> AddModifier(Workflow workflow, string nodePuid, string modifierPuid);
        Task<WorkflowChartResponse> RemoveModifier(Workflow workflow, string nodePuid, string modifierPuid);
        Task<WorkflowChartResponse> SetActualMachineCount(Workflow workflow, string nodePuid, int actualMachineCount);
        Task<WorkflowChartResponse> SetExternal(Workflow workflow, string nodePuid, bool isExternal);
        Task<WorkflowChartResponse> SetExternalRate(Workflow workflow, string nodePuid, double externalRate);
    }
}
