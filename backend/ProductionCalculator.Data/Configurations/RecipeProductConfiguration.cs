using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class RecipeProductConfiguration : IEntityTypeConfiguration<RecipeProduct>
    {
        public void Configure(EntityTypeBuilder<RecipeProduct> builder)
        {
            builder.ToTable("recipe_products", schema: "app");
            builder.Property(u => u.Recipe_Product_Id)
                .HasColumnName("recipe_product_id")
                .ValueGeneratedOnAdd();
            builder.HasKey(u => u.Recipe_Product_Id).HasName("recipe_products_pkey");
            builder.Property(u => u.Recipe_Id)
                .HasColumnName("recipe_id")
                .IsRequired();
            builder.Property(u => u.Product_Id)
                .HasColumnName("product_id")
                .IsRequired();
            builder.Property(u => u.Quantity)
                .HasColumnName("quantity")
                .IsRequired()
                .HasColumnType("numeric(10, 2)");
            builder.Property(u => u.Is_Input)
                .HasColumnName("is_input")
                .IsRequired();
        }
    }
}
