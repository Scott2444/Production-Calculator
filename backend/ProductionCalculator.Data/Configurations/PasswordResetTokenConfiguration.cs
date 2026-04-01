using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
    {
        public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
        {
            builder.ToTable("password_reset_tokens", schema: "app");

            builder.HasKey(u => u.Reset_Id).HasName("password_reset_tokens_pkey");
            builder.Property(u => u.Reset_Id)
                .HasColumnName("reset_id")
                .ValueGeneratedOnAdd();

            builder.Property(u => u.User_Id)
                .HasColumnName("user_id")
                .IsRequired();

            builder.HasIndex(u => u.User_Id)
                .IsUnique()
                .HasDatabaseName("password_reset_tokens_user_id_key");

            builder.Property(u => u.Token_Hash)
                .HasColumnName("token_hash")
                .HasMaxLength(64)
                .IsRequired();

            builder.HasIndex(u => u.Token_Hash)
                .IsUnique()
                .HasDatabaseName("password_reset_tokens_token_hash_key");

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