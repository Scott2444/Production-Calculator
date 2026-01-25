using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class ProductionNodeStateConfiguration : IEntityTypeConfiguration<ProductionNodeState>
    {
        public void Configure(EntityTypeBuilder<ProductionNodeState> builder)
        {
            builder.ToTable("production_node_state", schema: "app");
            builder.HasKey(e => e.Node_Id).HasName("production_node_state_pkey");
            builder.Property(e => e.Node_Id)
                .HasColumnName("node_id")
                .IsRequired();
            builder.Property(e => e.Actual_Machine_Count)
                .HasColumnName("actual_machine_count")
                .HasColumnType("numeric(14, 6)")
                .HasDefaultValue(0)
                .IsRequired();
            builder.Property(e => e.External_Supply_Rate)
                .HasColumnName("external_supply_rate")
                .HasColumnType("numeric(14, 6)");
            builder.Property(e => e.Realized_Recipe_Rate)
                .HasColumnName("realized_recipe_rate")
                .HasColumnType("numeric(14, 6)")
                .HasDefaultValue(0)
                .IsRequired();
        }
    }
}
