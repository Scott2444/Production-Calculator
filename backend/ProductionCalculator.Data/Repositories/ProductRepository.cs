using System.Collections.Generic;
using System.Threading.Tasks;
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

        public async Task AddAsync(Product product)
        {
            await _db.Products.AddAsync(product);
            await _db.SaveChangesAsync();
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _db.Products.FindAsync(id);
        }

        public async Task<IEnumerable<Product>> ListAsync()
        {
            return await _db.Products.ToListAsync();
        }
    }
}
