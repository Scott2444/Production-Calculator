using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IProductService
    {
        Task<ServiceResult<Product>> AddProduct(string projectPuid, string name, string? description);
        Task<ServiceResult<Product>> GetProductByPuid(string projectPuid, string puid);
        Task<ServiceResult<List<Product>>> GetProductsByProjectPuid(string projectPuid);
        Task<ServiceResult<Product>> UpdateProduct(string projectPuid, string puid, string? name, string? description);
        Task<ServiceResult> DeleteProduct(string projectPuid, string puid);
    }
}