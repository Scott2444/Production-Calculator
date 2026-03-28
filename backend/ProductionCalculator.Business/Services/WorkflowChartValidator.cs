using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Business.Services
{
    public class WorkflowChartValidator : IWorkflowChartValidator
    {
        public WorkflowChartValidator() {}

        /// <summary>
        /// Checks the nodechart against the project objects to ensure all versions are up to date.
        /// This also serves as a check that all referenced objects still exist (e.g. if a recipe was deleted after the node chart was calculated).
        /// </summary>
        /// <param name="nodeChart">The node chart to check</param>
        /// <param name="projectObjects">The project objects to check against</param>
        /// <returns>True if the node chart is up to date, false if any referenced objects are missing or have version mismatches</returns>
        /// <remarks>
        /// This checks all recipes, machines, modifiers, and attributes referenced by the node chart to ensure they still exist and that the versions match the latest versions in the project objects.
        /// Attribute relations (links) are not checked even if the default link changes in Project Data.
        /// </remarks>
        public bool WorkflowIsUpToDate(NodeChart nodeChart, ProjectObjects projectObjects)
        {
            // Nodes - Check that recipe version and machine? version are up to date with latest project version
            foreach (var fullNode in nodeChart.Nodes)
            {
                var recipe = projectObjects.Recipes.FirstOrDefault(r => r.Recipe_Id == fullNode.Node.Recipe_Id);
                if (recipe == null || fullNode.Node.Recipe_Version != recipe.Version)
                {
                    return false;
                }
                if (fullNode.Node.Machine_Id.HasValue)
                {
                    var machine = projectObjects.Machines.FirstOrDefault(m => m.Machine_Id == fullNode.Node.Machine_Id.Value);
                    if (machine == null || fullNode.Node.Machine_Version != machine.Version)
                    {
                        return false;
                    }
                }
                // Modifiers - Check that modifier versions are up to date with latest project version
                foreach (var workflowModifier in fullNode.Modifiers)
                {
                    var modifier = projectObjects.Modifiers.FirstOrDefault(m => m.Modifier_Id == workflowModifier.Modifier_Id);
                    if (modifier == null || workflowModifier.Modifier_Version != modifier.Version)
                    {
                        return false;
                    }
                }
            }

            // Products - Check that product still exists
            foreach (var productNode in nodeChart.ProductNodes)
            {
                var product = projectObjects.Products.FirstOrDefault(p => p.Product_Id == productNode.Product_Id);
                if (product == null)
                {
                    return false;
                }
            }

            // Preferred Recipes - Check that preferred recipe still exists
            foreach (var preferredRecipe in nodeChart.PreferredRecipes)
            {
                var recipe = projectObjects.Recipes.FirstOrDefault(r => r.Recipe_Id == preferredRecipe.Recipe_Id);
                if (recipe == null)
                {
                    return false;
                }
            }

            // Targets and edges do not need to be checked because they are dependent on the nodes and products which are already checked.
            return true;
        }
    }
}