using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class MachineRecipeConfiguration : IEntityTypeConfiguration<MachineRecipe>
    {
        public void Configure(EntityTypeBuilder<MachineRecipe> builder)
        {
            builder.ToTable("machine_recipes", schema: "app");
            builder.Property(u => u.Machine_Recipe_Id)
                .HasColumnName("machine_recipe_id")
                .ValueGeneratedOnAdd();
            builder.HasKey(u => u.Machine_Recipe_Id).HasName("machine_recipes_pkey");
            builder.Property(u => u.Machine_Id)
                .HasColumnName("machine_id")
                .IsRequired();
            builder.Property(u => u.Recipe_Id)
                .HasColumnName("recipe_id")
                .IsRequired();
        }
    }
}
