using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IProductRepository
    {
        Task AddProduct(Product product);
        Task<Product?> GetProductById(int id);
        Task<Product?> GetProductByPuid(string puid);
        Task<List<Product>> GetProductsByProjectId(int projectId);
        Task<Product> UpdateProduct(Product product);
        Task<bool> DeleteProduct(int id);
        Task<bool> PuidExists(string puid);
    }
}