
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Configurations
{
	public class WorkflowRecipeConfiguration : IEntityTypeConfiguration<WorkflowRecipe>
	{
		public void Configure(EntityTypeBuilder<WorkflowRecipe> builder)
		{
			builder.ToTable("workflow_recipes", schema: "app");
			builder.HasKey(r => r.Workflow_Recipe_Id).HasName("workflow_recipe_pkey");
			builder.Property(r => r.Workflow_Recipe_Id)
				.HasColumnName("workflow_recipe_id")
				.ValueGeneratedOnAdd();
			builder.Property(r => r.Workflow_Id)
				.HasColumnName("workflow_id")
				.IsRequired();
			builder.Property(r => r.Recipe_Id)
				.HasColumnName("recipe_id")
				.IsRequired();
		}
	}
}
