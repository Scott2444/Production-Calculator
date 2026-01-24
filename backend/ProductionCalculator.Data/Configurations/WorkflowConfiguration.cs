using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class WorkflowConfiguration : IEntityTypeConfiguration<Workflow>
    {
        public void Configure(EntityTypeBuilder<Workflow> builder)
        {
            builder.ToTable("workflows", schema: "app");
            builder.Property(u => u.Workflow_Id)
                .HasColumnName("workflow_id")
                .ValueGeneratedOnAdd();
            builder.HasKey(u => u.Workflow_Id).HasName("workflows_pkey");
            
            builder.Property(u => u.Project_Id)
                .HasColumnName("project_id")
                .IsRequired();

            builder.Property(u => u.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(u => u.Description)
                .HasColumnName("description");

            builder.Property(u => u.Created_At)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()")
                .IsRequired();
            
            builder.Property(u => u.Last_Updated)
                .HasColumnName("last_updated")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()")
                .IsRequired();
            
            builder.Property(u => u.Puid)
                .HasColumnName("puid")
                .HasColumnType("char(10)")
                .IsRequired();
        }
    }
}
