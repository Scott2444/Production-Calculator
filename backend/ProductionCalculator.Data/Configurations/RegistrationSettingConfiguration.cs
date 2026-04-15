using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class RegistrationSettingConfiguration : IEntityTypeConfiguration<RegistrationSetting>
    {
        public void Configure(EntityTypeBuilder<RegistrationSetting> builder)
        {
            builder.ToTable("registration_settings", schema: "app");

            builder.HasKey(s => s.Settings_Id).HasName("registration_settings_pkey");

            builder.Property(s => s.Settings_Id)
                .HasColumnName("settings_id")
                .ValueGeneratedNever();

            builder.Property(s => s.Is_Registration_Enabled)
                .HasColumnName("is_registration_enabled")
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(s => s.Last_Updated)
                .HasColumnName("last_updated")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()")
                .IsRequired();
        }
    }
}