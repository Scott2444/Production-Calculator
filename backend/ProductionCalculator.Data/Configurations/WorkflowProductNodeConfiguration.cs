using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
	public class WorkflowProductNodeConfiguration : IEntityTypeConfiguration<WorkflowProductNode>
	{
		public void Configure(EntityTypeBuilder<WorkflowProductNode> builder)
		{
			builder.ToTable("workflow_product_nodes", schema: "app");
			builder.HasKey(n => n.Workflow_Product_Node_Id).HasName("workflow_product_node_pkey");
			builder.Property(n => n.Workflow_Product_Node_Id)
				.HasColumnName("workflow_product_node_id")
				.ValueGeneratedOnAdd();
			builder.Property(n => n.Workflow_Id)
				.HasColumnName("workflow_id")
				.IsRequired();
			builder.Property(n => n.Product_Id)
				.HasColumnName("product_id")
				.IsRequired();
			builder.Property(n => n.Calculated_Flow_Rate)
				.HasColumnName("calculated_flow_rate")
				.HasColumnType("numeric(14, 6)")
				.IsRequired();
			builder.Property(n => n.Actual_Flow_Rate_In)
				.HasColumnName("actual_flow_rate_in")
				.HasColumnType("numeric(14, 6)")
				.IsRequired();
			builder.Property(n => n.Actual_Flow_Rate_Out)
				.HasColumnName("actual_flow_rate_out")
				.HasColumnType("numeric(14, 6)")
				.IsRequired();
			builder.Property(n => n.Is_External)
				.HasColumnName("is_external")
				.HasDefaultValue(false)
				.IsRequired();
		}
	}
}
