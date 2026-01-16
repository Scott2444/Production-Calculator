using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class VerificationCodeConfiguration : IEntityTypeConfiguration<VerificationCode>
    {
        public void Configure(EntityTypeBuilder<VerificationCode> builder)
        {
            builder.ToTable("verification_codes", schema: "app");
            builder.Property(u => u.Code_Id)
                .HasColumnName("code_id")
                .ValueGeneratedOnAdd();
            builder.HasKey(u => u.Code_Id).HasName("verification_codes_pkey");
            
            builder.Property(u => u.User_Id)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(u => u.Code_Hash)
                .HasColumnName("code_hash")
                .HasMaxLength(256)
                .IsRequired();
            
            builder.Property(u => u.Attempts)
                .HasColumnName("attempts")
                .IsRequired();
            
            builder.Property(u => u.Created_At)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()")
                .IsRequired();

            builder.Property(u => u.Expires_At)
                .HasColumnName("expires_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();
        }
    }
}
