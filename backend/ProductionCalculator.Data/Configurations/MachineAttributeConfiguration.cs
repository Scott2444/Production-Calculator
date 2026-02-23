using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class MachineAttributeConfiguration : IEntityTypeConfiguration<MachineAttribute>
    {
        public void Configure(EntityTypeBuilder<MachineAttribute> builder)
        {
            builder.ToTable("machine_attributes", schema: "app");
            builder.Property(u => u.Machine_Attribute_Id)
                .HasColumnName("machine_attribute_id")
                .ValueGeneratedOnAdd();
            builder.HasKey(u => u.Machine_Attribute_Id).HasName("machine_attributes_pkey");

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

            builder.Property(u => u.Version)
                .HasColumnName("version")
                .IsRequired()
                .HasDefaultValue(1);

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
        }
    }
}
