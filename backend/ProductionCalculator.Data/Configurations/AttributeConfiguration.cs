using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class AttributeConfiguration : IEntityTypeConfiguration<ProjectAttribute>
    {
        public void Configure(EntityTypeBuilder<ProjectAttribute> builder)
        {
            builder.ToTable("attributes", schema: "app");
            builder.Property(u => u.Attribute_Id)
                .HasColumnName("attribute_id")
                .ValueGeneratedOnAdd();
            builder.HasKey(u => u.Attribute_Id).HasName("attributes_pkey");

            builder.Property(u => u.Project_Id)
                .HasColumnName("project_id")
                .IsRequired();

            builder.Property(u => u.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(u => u.Description)
                .HasColumnName("description");

            builder.Property(u => u.Unit)
                .HasColumnName("unit")
                .HasMaxLength(50);

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
                .HasDatabaseName("attributes_puid_key");

            builder.Property(u => u.Version)
                .HasColumnName("version")
                .IsRequired()
                .HasDefaultValue(1);
        }
    }
}
