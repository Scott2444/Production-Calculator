using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("roles", schema: "app");

            builder.Property(r => r.Role_Id)
                .HasColumnName("role_id")
                .ValueGeneratedOnAdd();

            builder.Property(r => r.Role_Name)
                .HasColumnName("role_name")
                .HasMaxLength(100)
                .IsRequired();
        }
    }
}
