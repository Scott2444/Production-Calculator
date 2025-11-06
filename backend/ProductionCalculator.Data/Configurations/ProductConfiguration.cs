using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            // Table and key
            builder.ToTable("products", schema: "app");
            builder.HasKey(p => p.Id).HasName("products_pkey");
            builder.Property(p => p.Id)
                .HasColumnName("product_id")
                .ValueGeneratedOnAdd(); // EF will use identity/sequence depending on provider

            // Name
            builder.Property(p => p.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            // Unique constraints
            builder.HasIndex(p => p.Name)
                .IsUnique()
                .HasDatabaseName("products_name_key");

            // Composite unique (project_id, name)
            builder.HasIndex(p => new { p.Project_Id, p.Name })
                .IsUnique()
                .HasDatabaseName("uq_products_project_name");

            // Description
            builder.Property(p => p.Description)
                .HasColumnName("description")
                .HasColumnType("text");

            // Product type fk column mapping
            builder.Property(p => p.Product_Type_Id)
                .HasColumnName("product_type_id")
                .IsRequired();

            // Project fk column mapping
            builder.Property(p => p.Project_Id)
                .HasColumnName("project_id");
        }
    }
}