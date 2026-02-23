using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class WorkflowModifierAttributeConfiguration : IEntityTypeConfiguration<WorkflowModifierAttribute>
    {
        public void Configure(EntityTypeBuilder<WorkflowModifierAttribute> builder)
        {
            builder.ToTable("workflow_modifier_attributes", schema: "app");
            builder.Property(u => u.Workflow_Modifier_Attribute_Id)
                .HasColumnName("workflow_modifier_attribute_id")
                .ValueGeneratedOnAdd();
            builder.HasKey(u => u.Workflow_Modifier_Attribute_Id).HasName("workflow_modifier_attributes_pkey");

            builder.Property(u => u.Workflow_Id)
                .HasColumnName("workflow_id")
                .IsRequired();

            builder.Property(u => u.Modifier_Id)
                .HasColumnName("modifier_id")
                .IsRequired();

            builder.Property(u => u.Attribute_Id)
                .HasColumnName("attribute_id")
                .IsRequired();

            builder.Property(u => u.Flat_Bonus)
                .HasColumnName("flat_bonus")
                .HasColumnType("numeric(13, 5)")
                .IsRequired();

            builder.Property(u => u.Percent_Bonus)
                .HasColumnName("percent_bonus")
                .HasColumnType("numeric(13, 5)")
                .IsRequired();

            builder.Property(u => u.Multiplicative_Bonus)
                .HasColumnName("multiplicative_bonus")
                .HasColumnType("numeric(13, 5)")
                .IsRequired();
        }
    }
}
