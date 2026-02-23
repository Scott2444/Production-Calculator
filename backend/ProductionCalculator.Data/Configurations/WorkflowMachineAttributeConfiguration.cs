using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class WorkflowMachineAttributeConfiguration : IEntityTypeConfiguration<WorkflowMachineAttribute>
    {
        public void Configure(EntityTypeBuilder<WorkflowMachineAttribute> builder)
        {
            builder.ToTable("workflow_machine_attributes", schema: "app");
            builder.Property(u => u.Workflow_Machine_Attribute_Id)
                .HasColumnName("workflow_machine_attribute_id")
                .ValueGeneratedOnAdd();
            builder.HasKey(u => u.Workflow_Machine_Attribute_Id).HasName("workflow_machine_attributes_pkey");

            builder.Property(u => u.Workflow_Id)
                .HasColumnName("workflow_id")
                .IsRequired();

            builder.Property(u => u.Machine_Id)
                .HasColumnName("machine_id")
                .IsRequired();

            builder.Property(u => u.Attribute_Id)
                .HasColumnName("attribute_id")
                .IsRequired();

            builder.Property(u => u.Rate)
                .HasColumnName("rate")
                .HasColumnType("numeric(13, 5)")
                .IsRequired();
        }
    }
}
