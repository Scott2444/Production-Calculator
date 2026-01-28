using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class WorkflowTargetConfiguration : IEntityTypeConfiguration<WorkflowTarget>
    {
        public void Configure(EntityTypeBuilder<WorkflowTarget> builder)
        {
            builder.ToTable("workflow_targets", schema: "app");
            builder.HasKey(e => e.Workflow_Target_Id).HasName("workflow_target_pkey");
            builder.Property(e => e.Workflow_Target_Id)
                .HasColumnName("workflow_target_id")
                .ValueGeneratedOnAdd();
            builder.Property(e => e.Workflow_Id)
                .HasColumnName("workflow_id")
                .IsRequired();
            builder.Property(e => e.Product_Id)
                .HasColumnName("product_id")
                .IsRequired();
            builder.Property(e => e.Target_Rate)
                .HasColumnName("target_rate")
                .HasColumnType("numeric(14, 6)")
                .IsRequired();
        }
    }
}
