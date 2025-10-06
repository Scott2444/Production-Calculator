using System.Collections.Generic;
using System.Threading.Tasks;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(int id);
        Task<IEnumerable<Product>> ListAsync();
        Task AddAsync(Product product);
    }
}
