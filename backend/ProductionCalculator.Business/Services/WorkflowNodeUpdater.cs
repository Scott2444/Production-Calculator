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
        /// </remarks>
        /// <see cref="documentation/CalculationDesign.md"/>>
        public NodeUpdateImpact ApplyPutUpdate(FullNode fullNode, WorkflowNodeRequest request, ProjectObjects projectObjects)
        {
            bool requiresSupplyRecalc = false;
            bool requiresDemandRecalc = false;

            // Capture initial state for impact calculation before applying updates
            var previousModifierIds = fullNode.Modifiers.Select(m => m.Modifier_Id).ToList();
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
            fullNode.Modifiers = BuildModifiers(fullNode.Node.Node_Id, request.ModifierPuids, projectObjects);

            // Calculate Impact
            var currentModifierIds = fullNode.Modifiers.Select(m => m.Modifier_Id).ToList();
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
        /// Since this is a PUT, we assume the request contains the full desired state of modifiers for the node, and we overwrite the existing list with the new one. 
        /// </summary>
        private List<WorkflowNodeModifier> BuildModifiers(int nodeId, List<string> requestModifiers, ProjectObjects projectObjects)
        {
            var newModifiers = new List<WorkflowNodeModifier>();
            foreach (var requestModifier in requestModifiers)
            {
                var modifier = projectObjects.Modifiers.First(m => m.Puid == requestModifier);
                newModifiers.Add(new WorkflowNodeModifier
                {
                    Workflow_Node_Modifier_Id = 0, // Id will be set when saved to DB
                    Workflow_Node_Id = nodeId,
                    Modifier_Id = modifier.Modifier_Id,
                    Modifier_Version = modifier.Version
                });
            }
            return newModifiers;
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
