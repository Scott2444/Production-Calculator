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

            builder.Property(u => u.Flat_Bonus)
                .HasColumnName("flat_bonus")
                .HasColumnType("Numeric(13, 5)")
                .IsRequired();
            
            builder.Property(u => u.Percent_Bonus)
                .HasColumnName("percent_bonus")
                .HasColumnType("Numeric(13, 5)")
                .IsRequired();
            
            builder.Property(u => u.Multiplicative_Bonus)
                .HasColumnName("multiplicative_bonus")
                .HasColumnType("Numeric(13, 5)")
                .IsRequired();

            builder.Property(u => u.Input_Percent)
                .HasColumnName("input_percent")
                .HasColumnType("Numeric(13, 5)")
                .IsRequired();

            builder.Property(u => u.Output_Percent)
                .HasColumnName("output_percent")
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
                
            builder.Property(u => u.Version)
                .HasColumnName("version")
                .IsRequired()
                .HasDefaultValue(1);
        }
    }
}
