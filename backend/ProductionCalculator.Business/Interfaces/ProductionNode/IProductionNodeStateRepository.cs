using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IProductionNodeStateRepository
    {
        Task<ProductionNodeState?> GetByNodeId(int nodeId, bool isTracked = false);
        Task AddProductionNodeStates(List<ProductionNodeState> productionNodeStates);
        Task<List<ProductionNodeState>> UpdateProductionNodeStates(List<ProductionNodeState> productionNodeStates);
        Task<bool> DeleteProductionNodeStates(List<int> ids);
    }
}