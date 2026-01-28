using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class WorkflowNodeModifierConfiguration : IEntityTypeConfiguration<WorkflowNodeModifier>
    {
        public void Configure(EntityTypeBuilder<WorkflowNodeModifier> builder)
        {
            builder.ToTable("workflow_node_modifiers", schema: "app");
            builder.HasKey(e => e.Workflow_Node_Modifier_Id).HasName("workflow_node_modifiers_pkey");
            builder.Property(e => e.Workflow_Node_Modifier_Id)
                .HasColumnName("workflow_node_modifier_id")
                .ValueGeneratedOnAdd();
            builder.Property(e => e.Workflow_Node_Id)
                .HasColumnName("workflow_node_id")
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
