using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class MachineConfiguration : IEntityTypeConfiguration<Machine>
    {
        public void Configure(EntityTypeBuilder<Machine> builder)
        {
            builder.ToTable("machines", schema: "app");
            builder.Property(u => u.Machine_Id)
                .HasColumnName("machine_id")
                .ValueGeneratedOnAdd();
            builder.HasKey(u => u.Machine_Id).HasName("machines_pkey");
            
            builder.Property(u => u.Project_Id)
                .HasColumnName("project_id")
                .IsRequired();

            builder.Property(u => u.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(u => u.Description)
                .HasColumnName("description");

            builder.Property(u => u.Base_Speed)
                .HasColumnName("base_speed")
                .HasColumnType("Numeric(10, 2)")
                .IsRequired();

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
            builder.HasIndex(u => u.Puid)
                .IsUnique()
                .HasDatabaseName("machines_puid_key");
        }
    }
}
