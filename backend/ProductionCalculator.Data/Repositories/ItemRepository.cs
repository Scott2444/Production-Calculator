using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
    public class ItemRepository : IItemRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public ItemRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Item item)
        {
            await _db.Items.AddAsync(item);
            await _db.SaveChangesAsync();
        }

        public async Task<Item?> GetByIdAsync(int id)
        {
            return await _db.Items.FindAsync(id);
        }

        public async Task<IEnumerable<Item>> ListAsync()
        {
            return await _db.Items.ToListAsync();
        }
    }
}
