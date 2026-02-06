using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
    public class WorkflowEdgeConfiguration : IEntityTypeConfiguration<WorkflowEdge>
    {
        public void Configure(EntityTypeBuilder<WorkflowEdge> builder)
        {
            builder.ToTable("workflow_edges", schema: "app");
            builder.HasKey(e => e.Workflow_Edge_Id).HasName("workflow_edge_pkey");
            builder.Property(e => e.Workflow_Edge_Id)
                .HasColumnName("workflow_edge_id")
                .ValueGeneratedOnAdd();
            builder.Property(e => e.Workflow_Id)
                .HasColumnName("workflow_id")
                .IsRequired();
            builder.Property(e => e.Producer_Node_Id)
                .HasColumnName("producer_node_id");
            builder.Property(e => e.Consumer_Node_Id)
                .HasColumnName("consumer_node_id");
            builder.Property(e => e.Product_Node_Id)
                .HasColumnName("product_node_id")
                .IsRequired();
            builder.Property(e => e.Calculated_Flow_Rate)
                .HasColumnName("calculated_flow_rate")
                .HasColumnType("numeric(14, 6)")
                .IsRequired();
            builder.Property(e => e.Actual_Flow_Rate)
                .HasColumnName("actual_flow_rate")
                .HasColumnType("numeric(14, 6)")
                .IsRequired();
        }
    }
}
