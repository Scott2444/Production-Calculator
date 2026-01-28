using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class WorkflowNodeConfiguration : IEntityTypeConfiguration<WorkflowNode>
    {
        public void Configure(EntityTypeBuilder<WorkflowNode> builder)
        {
            builder.ToTable("workflow_nodes", schema: "app");
            builder.HasKey(e => e.Node_Id).HasName("workflow_nodes_pkey");
            builder.Property(e => e.Node_Id)
                .HasColumnName("workflow_node_id")
                .ValueGeneratedOnAdd();
            builder.Property(e => e.Workflow_Id)
                .HasColumnName("workflow_id")
                .IsRequired();
            builder.Property(e => e.Puid)
                .HasColumnName("puid")
                .IsRequired();
            builder.Property(e => e.Recipe_Id)
                .HasColumnName("recipe_id")
                .IsRequired();
            builder.Property(e => e.Recipe_Version)
                .HasColumnName("recipe_version")
                .IsRequired();
            builder.Property(e => e.Is_Preferred)
                .HasColumnName("is_preferred")
                .HasDefaultValue(false)
                .IsRequired();
            builder.Property(e => e.Machine_Id)
                .HasColumnName("machine_id");
            builder.Property(e => e.Machine_Version)
                .HasColumnName("machine_version");
            builder.Property(e => e.Actual_Machine_Count)
                .HasColumnName("actual_machine_count");
            builder.Property(e => e.Calculated_Machine_Count)
                .HasColumnName("calculated_machine_count");
            builder.Property(e => e.Calculated_Target_Rate)
                .HasColumnName("calculated_target_rate");
            builder.Property(e => e.Calculated_Actual_Rate)
                .HasColumnName("calculated_actual_rate");
        }
    }
}
