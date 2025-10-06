using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Data.Configurations;

namespace ProductionCalculator.Data
{
    public class ProductionCalculatorDbContext : DbContext
    {
        public ProductionCalculatorDbContext(DbContextOptions<ProductionCalculatorDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new ProductConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());

            // Configure Product.User_Attributes as jsonb using System.Text.Json serialization
            var options = new System.Text.Json.JsonSerializerOptions();

            var converter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<UserAttributes?, string?>(
                v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, options),
                v => string.IsNullOrEmpty(v) ? null : System.Text.Json.JsonSerializer.Deserialize<UserAttributes>(v, options)
            );

            var comparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<UserAttributes?>(
                (a, b) => System.Text.Json.JsonSerializer.Serialize(a, options) == System.Text.Json.JsonSerializer.Serialize(b, options),
                v => v == null ? 0 : System.Text.Json.JsonSerializer.Serialize(v, options)!.GetHashCode(),
                v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<UserAttributes>(System.Text.Json.JsonSerializer.Serialize(v, options), options)
            );

            modelBuilder.Entity<Product>()
                .Property(p => p.User_Attributes)
                .HasConversion(converter)
                .HasColumnName("user_attributes")
                .HasColumnType("jsonb")
                .Metadata.SetValueComparer(comparer);
        }
    }
}
