using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class ProductionNodeConfiguration : IEntityTypeConfiguration<ProductionNode>
    {
        public void Configure(EntityTypeBuilder<ProductionNode> builder)
        {
            builder.ToTable("production_nodes", schema: "app");
            builder.HasKey(e => e.Node_Id).HasName("production_nodes_pkey");
            builder.Property(e => e.Node_Id)
                .HasColumnName("node_id")
                .ValueGeneratedOnAdd();
            builder.Property(e => e.Workflow_Id)
                .HasColumnName("workflow_id")
                .IsRequired();
            builder.Property(e => e.Puid)
                .HasColumnName("puid")
                .HasColumnType("char(10)")
                .IsRequired();
            builder.HasIndex(e => e.Puid)
                .IsUnique()
                .HasDatabaseName("production_nodes_puid_key");
            builder.Property(e => e.Product_Id)
                .HasColumnName("product_id")
                .IsRequired();
            builder.Property(e => e.Product_Version)
                .HasColumnName("product_version")
                .IsRequired();
            builder.Property(e => e.Recipe_Id)
                .HasColumnName("recipe_id");
            builder.Property(e => e.Recipe_Version)
                .HasColumnName("recipe_version")
                .IsRequired();
            builder.Property(e => e.Machine_Id)
                .HasColumnName("machine_id");
            builder.Property(e => e.Machine_Version)
                .HasColumnName("machine_version")
                .IsRequired();
            builder.Property(e => e.Parent_Node_Id)
                .HasColumnName("parent_node_id");
            builder.Property(e => e.Target_Rate)
                .HasColumnName("target_rate")
                .HasColumnType("numeric(14, 6)")
                .IsRequired();
            builder.Property(e => e.Ideal_Machine_Count)
                .HasColumnName("ideal_machine_count")
                .HasColumnType("numeric(14, 6)")
                .IsRequired();
            builder.Property(e => e.Is_Root)
                .HasColumnName("is_root")
                .HasDefaultValue(false)
                .IsRequired();
            builder.Property(e => e.Is_External)
                .HasColumnName("is_external")
                .HasDefaultValue(false)
                .IsRequired();
            builder.Property(e => e.Created_At)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()")
                .IsRequired();
            builder.Property(e => e.Last_Updated)
                .HasColumnName("last_updated")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()")
                .IsRequired();
        }
    }
}
