using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IProductionNodeService
    {
        Task<IEnumerable<FullProductionNode>> GetByWorkflowId(int workflowId, bool isTracked = false);
        Task CompleteUpdateProductionNodes(int workflowId, IEnumerable<FullProductionNode> productionNodes);
    }
}
