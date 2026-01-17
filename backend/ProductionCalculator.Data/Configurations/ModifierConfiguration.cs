using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class ModifierConfiguration : IEntityTypeConfiguration<Modifier>
    {
        public void Configure(EntityTypeBuilder<Modifier> builder)
        {
            builder.ToTable("modifiers", schema: "app");
            builder.Property(u => u.Modifier_Id)
                .HasColumnName("modifier_id")
                .ValueGeneratedOnAdd();
            builder.HasKey(u => u.Modifier_Id).HasName("modifiers_pkey");
            
            builder.Property(u => u.Project_Id)
                .HasColumnName("project_id")
                .IsRequired();

            builder.Property(u => u.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(u => u.Description)
                .HasColumnName("description");

            builder.Property(u => u.Flat_Speed_Bonus)
                .HasColumnName("flat_speed_bonus")
                .HasColumnType("Numeric(13, 5)")
                .IsRequired();
            
            builder.Property(u => u.Additive_Percent_Bonus)
                .HasColumnName("additive_percent_bonus")
                .HasColumnType("Numeric(13, 5)")
                .IsRequired();
            
            builder.Property(u => u.Multiplicative_Modifiers)
                .HasColumnName("multiplicative_modifiers")
                .HasColumnType("Numeric(13, 5)")
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
        }
    }
}
