using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Records;

namespace ProductionCalculator.Business.Services
{
    public class WorkflowNodeUpdater : IWorkflowNodeUpdater
    {

        private const double YIELD_TOLERANCE = 1e-9;

        /// <summary>
        /// Applies the updates from a WorkflowNodeRequest to a FullNode, and determines whether the changes require a supply or demand recalculation.
        /// </summary>
        /// <param name="fullNode">Existing FullNode to be updated, will be modified</param>
        /// <param name="request">Changes to be applied to the fullNode</param>
        /// <param name="projectObjects">Project Data</param>
        /// <returns>(RequiresDemandRecalculation, RequiresSupplyRecalculation)</returns>
        /// <remarks>
        /// Recalculates demand if changes to modifiers cause a change in yield (input/output percent). This is because yield changes can affect the amount of inputs required and outputs produced, which directly impacts demand.
        /// Recalculates supply if changes are made to machine, modifiers, and actual_machine_count that affect speed.
        /// Attributes do not trigger recalculations.
        /// </remarks>
        /// <see cref="documentation/CalculationDesign.md"/>>
        public NodeUpdateImpact ApplyPutUpdate(FullNode fullNode, WorkflowNodeRequest request, ProjectObjects projectObjects)
        {
            bool requiresSupplyRecalc = false;
            bool requiresDemandRecalc = false;

            // Capture initial state for impact calculation before applying updates
            var previousModifierIds = fullNode.Modifiers.Select(m => m.Modifier.Modifier_Id).ToList();
            var previousYield = ComputeNodeYieldSums(previousModifierIds, projectObjects);
            var previousMachineId = fullNode.Node.Machine_Id;
            var previousMachineCount = fullNode.Node.Actual_Machine_Count;

            // Apply update
            
            // Overwrite Machine
            var machine = projectObjects.Machines.First(m => m.Puid == request.MachinePuid);
            fullNode.Node.Machine_Id = machine.Machine_Id;
            fullNode.Node.Machine_Version = machine.Version;
            fullNode.Node.Actual_Machine_Count = request.ActualMachineCount;

            // Overwrite Modifiers (Helper method builds the list from scratch based on request)
            fullNode.Modifiers = BuildModifiers(fullNode.Node.Node_Id, request.Modifiers, projectObjects);

            // Overwrite Attributes
            fullNode.RecipeAttributes = BuildRecipeAttributes(fullNode.Node.Node_Id, request.RecipeAttributes, projectObjects);
            fullNode.MachineAttributes = BuildMachineAttributes(fullNode.Node.Node_Id, request.MachineAttributes, projectObjects);

            // Calculate Impact
            var currentModifierIds = fullNode.Modifiers.Select(m => m.Modifier.Modifier_Id).ToList();
            var currentYield = ComputeNodeYieldSums(currentModifierIds, projectObjects);

            if (!YieldEquals(previousYield, currentYield))
            {
                requiresDemandRecalc = true; 
            }
            else if (previousMachineId != fullNode.Node.Machine_Id || 
                    previousMachineCount != fullNode.Node.Actual_Machine_Count ||
                    !previousModifierIds.ToHashSet().SetEquals(currentModifierIds.ToHashSet()))
            {
                requiresSupplyRecalc = true;
            }

            return new NodeUpdateImpact(requiresDemandRecalc, requiresSupplyRecalc);
        }

        /// <summary>
        /// Builds a new list of modifiers for the node based on the request.
        /// This involves mapping the request modifiers to the internal representation, and also adding any default attributes for newly added modifiers. 
        /// Since this is a PUT, we assume the request contains the full desired state of modifiers for the node, and we overwrite the existing list with the new one. 
        /// </summary>
        private List<FullWorkflowModifier> BuildModifiers(int nodeId, List<WorkflowModifierExchange> requestModifiers, ProjectObjects projectObjects)
        {
            var newModifiers = new List<FullWorkflowModifier>();
            foreach (var requestModifier in requestModifiers)
            {
                var modifier = projectObjects.Modifiers.First(m => m.Puid == requestModifier.Puid);
                var modifierAttributes = new List<WorkflowModifierAttribute>();

                foreach (var requestAttribute in requestModifier.Attributes)
                {
                    var attribute = projectObjects.Attributes.First(a => a.Puid == requestAttribute.AttributePuid);
                    modifierAttributes.Add(new WorkflowModifierAttribute
                    {
                        Workflow_Modifier_Attribute_Id = 0, // Id will be set when saved to DB
                        Workflow_Node_Id = nodeId,
                        Workflow_Node_Modifier_Id = 0, // Will be set in WorkflowChartDataService after modifiers are saved and have their IDs
                        Modifier_Id = modifier.Modifier_Id,
                        Attribute_Id = attribute.Attribute_Id,
                        Flat_Bonus = requestAttribute.FlatBonus,
                        Percent_Bonus = requestAttribute.PercentBonus,
                        Multiplicative_Bonus = requestAttribute.MultiplicativeBonus
                    });
                }

                newModifiers.Add(new FullWorkflowModifier
                {
                    Modifier = new WorkflowNodeModifier
                    {
                        Workflow_Node_Modifier_Id = 0, // Id will be set when saved to DB
                        Workflow_Node_Id = nodeId,
                        Modifier_Id = modifier.Modifier_Id,
                        Modifier_Version = modifier.Version
                    },
                    ModifierAttributes = modifierAttributes
                });
            }
            return newModifiers;
        }

        /// <summary>
        /// Builds a new list of recipe attributes for the node based on the request. 
        /// Similar to modifiers, we overwrite the existing list with the new one from the request.
        /// </summary>
        private List<WorkflowRecipeAttribute> BuildRecipeAttributes(int nodeId, List<AttributeRateExchange> requestModifiers, ProjectObjects projectObjects)
        {
            var newAttributes = new List<WorkflowRecipeAttribute>();
            foreach (var requestAttribute in requestModifiers)
            {
                var attribute = projectObjects.Attributes.First(a => a.Puid == requestAttribute.Puid);
                newAttributes.Add(new WorkflowRecipeAttribute
                {
                    Workflow_Recipe_Attribute_Id = 0, // Id will be set when saved to DB
                    Workflow_Node_Id = nodeId,
                    Attribute_Id = attribute.Attribute_Id,
                    Rate = requestAttribute.Rate
                });
            }
            return newAttributes;
        }

        /// <summary>
        /// Builds a new list of machine attributes for the node based on the request. 
        /// Similar to modifiers, we overwrite the existing list with the new one from the request.
        /// </summary>
        private List<WorkflowMachineAttribute> BuildMachineAttributes(int nodeId, List<AttributeRateExchange> requestModifiers, ProjectObjects projectObjects)
        {
            var newAttributes = new List<WorkflowMachineAttribute>();
            foreach (var requestAttribute in requestModifiers)
            {
                var attribute = projectObjects.Attributes.First(a => a.Puid == requestAttribute.Puid);
                newAttributes.Add(new WorkflowMachineAttribute
                {
                    Workflow_Machine_Attribute_Id = 0, // Id will be set when saved to DB
                    Workflow_Node_Id = nodeId,
                    Attribute_Id = attribute.Attribute_Id,
                    Rate = requestAttribute.Rate
                });
            }
            return newAttributes;
        }


        private static (double inputPercent, double outputPercent) ComputeNodeYieldSums(List<int> modifierIds, ProjectObjects projectObjects)
        {
            var modifiers = projectObjects.Modifiers.Where(m => modifierIds.Contains(m.Modifier_Id));
            return (
                inputPercent: modifiers.Sum(m => m.Input_Percent),
                outputPercent: modifiers.Sum(m => m.Output_Percent)
            );
        }

        private static bool YieldEquals((double inputPercent, double outputPercent) left, (double inputPercent, double outputPercent) right)
        {
            return Math.Abs(left.inputPercent - right.inputPercent) < YIELD_TOLERANCE
                && Math.Abs(left.outputPercent - right.outputPercent) < YIELD_TOLERANCE;
        }
    }
}
