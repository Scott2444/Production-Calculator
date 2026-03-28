using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> builder)
        {
            builder.ToTable("projects", schema: "app");

            builder.Property(u => u.Project_Id)
                .HasColumnName("project_id")
                .ValueGeneratedOnAdd();
            builder.HasKey(u => u.Project_Id).HasName("projects_pkey");
            
            builder.Property(u => u.User_Id)
                .HasColumnName("user_id")
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
            builder.HasIndex(u => u.Puid)
                .IsUnique()
                .HasDatabaseName("projects_puid_key");

            builder.Property(u => u.Is_Public)
                .HasColumnName("is_public")
                .IsRequired()
                .HasDefaultValue(false);
            
            builder.Property(u => u.Alias_Project_Puid)
                .HasColumnName("alias_project_puid");

            builder.Property(u => u.Alias_Count)
                .HasColumnName("alias_count")
                .IsRequired()
                .HasDefaultValue(0);
        }
    }
}
