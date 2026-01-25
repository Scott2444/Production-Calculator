using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class ProductionNodeModifierConfiguration : IEntityTypeConfiguration<ProductionNodeModifier>
    {
        public void Configure(EntityTypeBuilder<ProductionNodeModifier> builder)
        {
            builder.ToTable("production_node_modifiers", schema: "app");
            builder.HasKey(e => e.Node_Modifier_Id).HasName("production_node_modifiers_pkey");
            builder.Property(e => e.Node_Modifier_Id)
                .HasColumnName("node_modifier_id")
                .ValueGeneratedOnAdd();
            builder.Property(e => e.Node_Id)
                .HasColumnName("node_id")
                .IsRequired();
            builder.Property(e => e.Modifier_Id)
                .HasColumnName("modifier_id")
                .IsRequired();
            builder.Property(e => e.Modifier_Version)
                .HasColumnName("modifier_version")
                .IsRequired();
        }
    }
}
