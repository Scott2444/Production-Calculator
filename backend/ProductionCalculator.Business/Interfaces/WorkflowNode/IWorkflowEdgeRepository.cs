using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IWorkflowEdgeRepository
    {
        Task<List<WorkflowEdge>> GetByWorkflow(int workflowId, bool isTracked = false);
        Task AddWorkflowEdges(List<WorkflowEdge> workflowEdges);
        Task UpdateWorkflowEdges(List<WorkflowEdge> workflowEdges);
        Task<bool> DeleteWorkflowEdges(List<int> ids);
    }
}