using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IProductionNodeInputRepository
    {
        Task<List<ProductionNodeInput>> GetByNodeId(int nodeId, bool isTracked = false);
        Task AddProductionNodeInputs(List<ProductionNodeInput> productionNodeInputs);
        Task<List<ProductionNodeInput>> UpdateProductionNodeInputs(List<ProductionNodeInput> productionNodeInputs);
        Task<bool> DeleteProductionNodeInputs(List<int> ids);
    }
}