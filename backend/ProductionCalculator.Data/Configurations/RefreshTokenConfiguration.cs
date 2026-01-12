using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("refresh_tokens", schema: "app");
            builder.Property(u => u.Token_Id)
                .HasColumnName("token_id")
                .ValueGeneratedOnAdd();
            builder.HasKey(u => u.Token_Id).HasName("refresh_tokens_pkey");
            
            builder.Property(u => u.User_Id)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(u => u.Token)
                .HasColumnName("token")
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(u => u.Expires_At)
                .HasColumnName("expires_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(u => u.Created_At)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()")
                .IsRequired();

            builder.Property(u => u.Revoked_At)
                .HasColumnName("revoked_at")
                .HasColumnType("timestamp with time zone");
        }
    }
}
