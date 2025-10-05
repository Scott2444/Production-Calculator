using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data
{
    public class ProductionCalculatorDbContext : DbContext
    {
        public ProductionCalculatorDbContext(DbContextOptions<ProductionCalculatorDbContext> options)
            : base(options)
        {
        }

        public DbSet<Item> Items => Set<Item>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new ItemConfiguration());
        }
    }
}
