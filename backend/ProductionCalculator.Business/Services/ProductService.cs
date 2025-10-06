using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Business.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;

        public ProductService(IProductRepository repo)
        {
            _repo = repo;
        }

        public async Task<Product> CreateAsync(Product product)
        {
            if (string.IsNullOrWhiteSpace(product.Name))
                throw new ArgumentException("Product name is required", nameof(product));

            await _repo.AddAsync(product);
            return product;
        }

        public async Task<IEnumerable<Product>> GetAllAsync() => await _repo.ListAsync();

        public async Task<Product?> GetByIdAsync(int id) => await _repo.GetByIdAsync(id);
    }
}
