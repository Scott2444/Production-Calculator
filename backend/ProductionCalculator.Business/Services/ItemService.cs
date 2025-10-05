using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Business.Services
{
    public class ItemService : IItemService
    {
        private readonly IItemRepository _repo;

        public ItemService(IItemRepository repo)
        {
            _repo = repo;
        }

        public async Task<Item> CreateAsync(Item item)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
                throw new ArgumentException("Item name is required", nameof(item));

            await _repo.AddAsync(item);
            return item;
        }

        public async Task<IEnumerable<Item>> GetAllAsync() => await _repo.ListAsync();

        public async Task<Item?> GetByIdAsync(int id) => await _repo.GetByIdAsync(id);
    }
}
