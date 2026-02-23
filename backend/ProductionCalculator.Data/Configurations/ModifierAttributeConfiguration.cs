using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class ModifierAttributeConfiguration : IEntityTypeConfiguration<ModifierAttribute>
    {
        public void Configure(EntityTypeBuilder<ModifierAttribute> builder)
        {
            builder.ToTable("modifier_attributes", schema: "app");
            builder.Property(u => u.Modifier_Attribute_Id)
                .HasColumnName("modifier_attribute_id")
                .ValueGeneratedOnAdd();
            builder.HasKey(u => u.Modifier_Attribute_Id).HasName("modifier_attributes_pkey");

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
