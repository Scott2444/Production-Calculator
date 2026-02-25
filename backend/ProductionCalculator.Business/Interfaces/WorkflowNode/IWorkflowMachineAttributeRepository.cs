using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IWorkflowMachineAttributeRepository
    {
        Task<List<WorkflowMachineAttribute>> GetByNodeId(int workflowNodeId, bool isTracked = false);
        Task AddWorkflowMachineAttributes(List<WorkflowMachineAttribute> workflowMachineAttributes);
        Task UpdateWorkflowMachineAttributes(List<WorkflowMachineAttribute> workflowMachineAttributes);
        Task<bool> DeleteWorkflowMachineAttributes(List<int> ids);
    }
}
