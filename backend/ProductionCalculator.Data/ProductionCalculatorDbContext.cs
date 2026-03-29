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

        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new RoleConfiguration());
            modelBuilder.ApplyConfiguration(new ProjectConfiguration());
            modelBuilder.ApplyConfiguration(new ProductConfiguration());
            modelBuilder.ApplyConfiguration(new AttributeConfiguration());
            modelBuilder.ApplyConfiguration(new RecipeConfiguration());
            modelBuilder.ApplyConfiguration(new RecipeProductConfiguration());
            modelBuilder.ApplyConfiguration(new RecipeAttributeConfiguration());
            modelBuilder.ApplyConfiguration(new MachineConfiguration());
            modelBuilder.ApplyConfiguration(new MachineRecipeConfiguration());
            modelBuilder.ApplyConfiguration(new MachineAttributeConfiguration());
            modelBuilder.ApplyConfiguration(new ModifierConfiguration());
            modelBuilder.ApplyConfiguration(new ModifierAttributeConfiguration());
            modelBuilder.ApplyConfiguration(new WorkflowConfiguration());
            modelBuilder.ApplyConfiguration(new WorkflowNodeConfiguration());
            modelBuilder.ApplyConfiguration(new WorkflowTargetConfiguration());
            modelBuilder.ApplyConfiguration(new WorkflowNodeModifierConfiguration());
            modelBuilder.ApplyConfiguration(new WorkflowEdgeConfiguration());
            modelBuilder.ApplyConfiguration(new WorkflowProductNodeConfiguration());
            modelBuilder.ApplyConfiguration(new WorkflowRecipeConfiguration());
            modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
            modelBuilder.ApplyConfiguration(new VerificationCodeConfiguration());

            if (!string.Equals(Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal))
            {
                modelBuilder.Entity<Project>().Ignore(project => project.Search_Vector);
            }
        }
    }
}
