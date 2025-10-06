using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.HasKey(u => u.User_Id).HasName("users_pkey");
            builder.Property(u => u.User_Id)
                .HasColumnName("user_id")
                .ValueGeneratedOnAdd();

            builder.Property(u => u.Username)
                .HasColumnName("username")
                .HasMaxLength(100)
                .IsRequired();

            builder.HasIndex(u => u.Username)
                .IsUnique()
                .HasDatabaseName("users_username_key");

            builder.Property(u => u.Email)
                .HasColumnName("email")
                .HasMaxLength(255)
                .IsRequired();

            builder.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("users_email_key");

            builder.Property(u => u.Password_Hash)
                .HasColumnName("password_hash")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(u => u.Created_At)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()")
                .IsRequired();
        }
    }
}
