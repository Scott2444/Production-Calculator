using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public ProductRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task AddProduct(Product product)
        {
            await _db.Set<Product>().AddAsync(product);
            await _db.SaveChangesAsync();
        }

        public async Task<Product?> GetProductById(int id)
        {
            return await _db.Set<Product>().FindAsync(id);
        }
        public async Task<Product?> GetProductByPuid(string puid)
        {
            return await _db.Set<Product>().FirstOrDefaultAsync(p => p.Puid == puid);
        }
        public async Task<List<Product>> GetProductsByProjectId(int projectId)
        {
            return await _db.Set<Product>().Where(p => p.Project_Id == projectId).ToListAsync();
        }
        public async Task<Product> UpdateProduct(Product product)
        {
            _db.Set<Product>().Update(product);
            await _db.SaveChangesAsync();
            return product;
        }
        public async Task<bool> DeleteProduct(int id) {
            var product = await _db.Set<Product>().FindAsync(id);
            if (product == null) return false;

            _db.Set<Product>().Remove(product);
            await _db.SaveChangesAsync();
            return true;
        }
        public async Task<bool> PuidExists(string puid)
        {
            return await _db.Set<Product>().AnyAsync(p => p.Puid == puid);
        }
    }
}
