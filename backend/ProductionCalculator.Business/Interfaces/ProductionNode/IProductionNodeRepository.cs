using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IProductionNodeRepository
    {
        Task<List<ProductionNode>> GetByWorkflowId(int workflowId, bool isTracked = false);
        Task AddProductionNodes(List<ProductionNode> productionNodes);
        Task<List<ProductionNode>> UpdateProductionNodes(List<ProductionNode> productionNodes);
        Task<bool> DeleteProductionNodes(List<int> ids);
        Task<bool> PuidExists(string puid);
    }
}