using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IProductionNodeModifierRepository
    {
        Task<List<ProductionNodeModifier>> GetByNodeId(int nodeId, bool isTracked = false);
        Task AddProductionNodeModifiers(List<ProductionNodeModifier> productionNodeModifiers);
        Task<List<ProductionNodeModifier>> UpdateProductionNodeModifiers(List<ProductionNodeModifier> productionNodeModifiers);
        Task<bool> DeleteProductionNodeModifiers(List<int> ids);
    }
}