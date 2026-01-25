using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class ProductionNodeInputConfiguration : IEntityTypeConfiguration<ProductionNodeInput>
    {
        public void Configure(EntityTypeBuilder<ProductionNodeInput> builder)
        {
            builder.ToTable("production_node_inputs", schema: "app");
            builder.HasKey(e => e.Node_Input_Id).HasName("production_node_inputs_pkey");
            builder.Property(e => e.Node_Input_Id)
                .HasColumnName("node_input_id")
                .ValueGeneratedOnAdd();
            builder.Property(e => e.Node_Id)
                .HasColumnName("node_id")
                .IsRequired();
            builder.Property(e => e.Input_Product_Id)
                .HasColumnName("input_product_id")
                .IsRequired();
            builder.Property(e => e.Required_Rate)
                .HasColumnName("required_rate")
                .HasColumnType("numeric(14, 6)")
                .IsRequired();
            builder.Property(e => e.Is_Cyclic)
                .HasColumnName("is_cyclic")
                .HasDefaultValue(false)
                .IsRequired();
        }
    }
}
