using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
    {
        public void Configure(EntityTypeBuilder<Recipe> builder)
        {
            builder.ToTable("recipes", schema: "app");

            builder.Property(u => u.Recipe_Id)
                .HasColumnName("recipe_id")
                .ValueGeneratedOnAdd();
            builder.HasKey(u => u.Recipe_Id).HasName("recipes_pkey");

            builder.Property(u => u.Project_Id)
                .HasColumnName("project_id")
                .IsRequired();

            builder.Property(u => u.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(u => u.Description)
                .HasColumnName("description");
            
            builder.Property(u => u.Base_Crafting_Time)
                .HasColumnName("base_crafting_time")
                .IsRequired()
                .HasColumnType("numeric(10, 2)");

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
            builder.HasIndex(u => u.Puid)
                .IsUnique()
                .HasDatabaseName("recipes_puid_key");
                
            builder.Property(u => u.Version)
                .HasColumnName("version")
                .IsRequired()
                .HasDefaultValue(1);
        }
    }
}
