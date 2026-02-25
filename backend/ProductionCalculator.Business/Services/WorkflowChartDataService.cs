using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Services
{
	public class WorkflowNodeDbService : IWorkflowChartDataService
	{
		private readonly IWorkflowNodeRepository _nodeRepo;
		private readonly IWorkflowTargetRepository _targetRepo;
        private readonly IWorkflowNodeModifierRepository _modifierRepo;
        private readonly IWorkflowRecipeAttributeRepository _recipeAttributeRepo;
        private readonly IWorkflowMachineAttributeRepository _machineAttributeRepo;
        private readonly IWorkflowModifierAttributeRepository _workflowModifierAttributeRepo;
        private readonly IWorkflowEdgeRepository _edgeRepo;
        private readonly IWorkflowProductNodeRepository _productNodeRepo;
        private readonly IWorkflowRecipeRepository _recipeRepo;

		public WorkflowNodeDbService(
            IWorkflowNodeRepository nodeRepo, 
            IWorkflowTargetRepository targetRepo, 
            IWorkflowNodeModifierRepository modifierRepo, 
            IWorkflowRecipeAttributeRepository recipeAttributeRepo,
            IWorkflowMachineAttributeRepository machineAttributeRepo,
            IWorkflowModifierAttributeRepository workflowModifierAttributeRepo,
            IWorkflowEdgeRepository edgeRepo,
            IWorkflowProductNodeRepository productNodeRepo,
            IWorkflowRecipeRepository recipeRepo
            )
		{
			_nodeRepo = nodeRepo;
			_targetRepo = targetRepo;
			_modifierRepo = modifierRepo;
            _recipeAttributeRepo = recipeAttributeRepo;
            _machineAttributeRepo = machineAttributeRepo;
            _workflowModifierAttributeRepo = workflowModifierAttributeRepo;
            _edgeRepo = edgeRepo;
            _productNodeRepo = productNodeRepo;
            _recipeRepo = recipeRepo;
		}

        /// <summary>
        /// Retrieves all nodes, edges, targets, and product nodes for a given workflow, along with their modifiers and attributes.
        /// </summary>
        /// <param name="workflowId"></param>
        /// <param name="isTracked">If true, the returned entities will be tracked by the EF context</param>
        /// <returns>Assembled NodeChart</returns>
        public async Task<NodeChart> GetByWorkflowId(int workflowId, bool isTracked = false)
        {
            var nodes = new List<FullNode>();
            var workflowNodes = await _nodeRepo.GetByWorkflow(workflowId, isTracked);
            foreach (var workflowNode in workflowNodes)
            {
                var modifiers = await _modifierRepo.GetByNodeId(workflowNode.Node_Id, isTracked);
                var modifierAttributes = await _workflowModifierAttributeRepo.GetByNodeId(workflowNode.Node_Id, isTracked);
                nodes.Add(new FullNode
                {
                    Node = workflowNode,
                    Modifiers = modifiers
                        .Select(m => new FullWorkflowModifier
                        {
                            Modifier = m,
                            ModifierAttributes = modifierAttributes
                                .Where(a => a.Workflow_Node_Modifier_Id == m.Workflow_Node_Modifier_Id)
                                .ToList()
                        })
                        .ToList(),
                    RecipeAttributes = await _recipeAttributeRepo.GetByNodeId(workflowNode.Node_Id, isTracked),
                    MachineAttributes = await _machineAttributeRepo.GetByNodeId(workflowNode.Node_Id, isTracked),
                });
            }
            var edges = await _edgeRepo.GetByWorkflow(workflowId, isTracked);
            var targets = await _targetRepo.GetByWorkflowId(workflowId, isTracked);
            var productNodes = await _productNodeRepo.GetByWorkflowId(workflowId, isTracked);
            var recipes = await _recipeRepo.GetByWorkflowId(workflowId, isTracked);
            return new NodeChart
            {
                Nodes = nodes,
                Edges = edges,
                Targets = targets,
                ProductNodes = productNodes,
                PreferredRecipes = recipes
            };
        }
        
        /// <summary>
        /// Handles the logic of determining which nodes need to be created, updated, or deleted, and performs those operations.
        /// Translates NodeChart to subcomponent and calls respective repos.
        /// Modifies NodeChart to reflect primary keys assigned as it is in the DB.
        /// Modifiers and attributes depend on nodes and this will automatically assign those node_ids to the modifiers.
        /// ModifierAttributes depend on modifiers and nodes, and this will automatically assign those node_ids and modifier_ids to the modifier attributes.
        /// </summary>
        /// <returns>Updated NodeChart with IDs assigned by the database</returns>
        public async Task<NodeChart> WorkflowUpdate(int workflowId, NodeChart nodeChart)
        {
            var originalChart = await GetByWorkflowId(workflowId, isTracked: false);
            
            await UpdateNodes(nodeChart.Nodes.Select(n => n.Node).ToList(), originalChart.Nodes.Select(n => n.Node).ToList());
            await UpdateTargets(nodeChart.Targets, originalChart.Targets);

            SetNodeIdDependencies(nodeChart); // Solves Node_Id dependency
            await UpdateModifiers(
                nodeChart.Nodes.SelectMany(n => n.Modifiers.Select(m => m.Modifier)).ToList(),
                originalChart.Nodes.SelectMany(n => n.Modifiers.Select(m => m.Modifier)).ToList());
            NormalizeAttributeReferences(nodeChart);
            await UpdateRecipeAttributes(nodeChart.Nodes.SelectMany(n => n.RecipeAttributes).ToList(), originalChart.Nodes.SelectMany(n => n.RecipeAttributes).ToList());
            await UpdateMachineAttributes(nodeChart.Nodes.SelectMany(n => n.MachineAttributes).ToList(), originalChart.Nodes.SelectMany(n => n.MachineAttributes).ToList());
            
            SetModifierIdsDependencies(nodeChart); // Solves ModifierAttribute -> Node and Modifier dependencies
            await UpdateModifierAttributes(
                nodeChart.Nodes.SelectMany(n => n.Modifiers.SelectMany(m => m.ModifierAttributes)).ToList(),
                originalChart.Nodes.SelectMany(n => n.Modifiers.SelectMany(m => m.ModifierAttributes)).ToList());
            await UpdateProductNodes(nodeChart.ProductNodes, originalChart.ProductNodes);
            await UpdateRecipes(nodeChart.PreferredRecipes, originalChart.PreferredRecipes);
            return nodeChart;
        }

        /// <summary>
        /// Checks if a node with the specified PUID exists.
        /// Used when creating nodes to ensure PUID uniqueness.
        /// </summary>
        public async Task<bool> NodePuidExists(string puid)
        {
            return await _nodeRepo.PuidExists(puid);
        }

        /// <summary>
        /// Handles the logic of determining which nodes need to be created, updated, or deleted, and performs those operations.
        /// Translates NodeChart to subcomponent and calls respective repos.
        /// Modifies NodeChart to reflect primary keys assigned as it is in the DB.
        /// Returns updated NodeChart.
        /// </summary>
        public async Task<NodeChart> WorkflowEdgeUpdate(int workflowId, NodeChart nodeChart)
        {
            var originalChart = await GetByWorkflowId(workflowId, isTracked: false);
            
            await UpdateEdges(nodeChart.Edges, originalChart.Edges);

            return nodeChart;
        }

        /// <summary>
        /// Handles the logic of determining which nodes need to be created, updated, or deleted, and performs those operations.
        /// Calls repos to perform DB operations.
        /// Does not handle modifiers in nodes.
        /// </summary>
        private async Task UpdateNodes(List<WorkflowNode> newNodes, List<WorkflowNode> originalNodes)
        {
            // Use dictionaries for O(1) lookups
            var originalDict = originalNodes.ToDictionary(n => n.Node_Id);
            // Limit newDict to only nodes that have an assigned Node_Id to avoid issues with new nodes having default 0 value
            var newDict = newNodes.Where(n => n.Node_Id != 0).ToDictionary(n => n.Node_Id);

            // Add: in new, not in original
            var inputsToAdd = newNodes.Where(nn => !originalDict.ContainsKey(nn.Node_Id)).ToList();
            // Update: in both, but values differ
            var inputsToUpdate = newNodes.Where(nn => originalDict.TryGetValue(nn.Node_Id, out var on) && !on.ValueEquals(nn)).ToList();
            // Delete: in original, not in new
            var inputsToDelete = originalDict.Values.Where(en => !newDict.ContainsKey(en.Node_Id)).ToList();

            // Add new
            if (inputsToAdd.Any()) await _nodeRepo.AddWorkflowNodes(inputsToAdd);

            // Update existing
            if (inputsToUpdate.Any()) await _nodeRepo.UpdateWorkflowNodes(inputsToUpdate);

            // Delete removed
            if (inputsToDelete.Any()) await _nodeRepo.DeleteWorkflowNodes(inputsToDelete.Select(i => i.Node_Id).ToList());
        }

        /// <summary>
        /// Handles the logic of determining which targets need to be created, updated, or deleted, and performs those operations.
        /// Calls repos to perform DB operations.
        /// </summary>
        private async Task UpdateTargets(List<WorkflowTarget> newTargets, List<WorkflowTarget> originalTargets)
        {
            // Use dictionaries for O(1) lookups
            var originalDict = originalTargets.ToDictionary(t => t.Workflow_Target_Id);
            // Limit newDict to only targets that have an assigned Workflow_Target_Id to avoid issues with new targets having default 0 value
            var newDict = newTargets.Where(t => t.Workflow_Target_Id != 0).ToDictionary(t => t.Workflow_Target_Id);

            // Add: in new, not in original
            var inputsToAdd = newTargets.Where(nt => !originalDict.ContainsKey(nt.Workflow_Target_Id)).ToList();
            // Update: in both, but values differ
            var inputsToUpdate = newTargets.Where(nt => originalDict.TryGetValue(nt.Workflow_Target_Id, out var ot) && !ot.ValueEquals(nt)).ToList();
            // Delete: in original, not in new
            var inputsToDelete = originalDict.Values.Where(et => !newDict.ContainsKey(et.Workflow_Target_Id)).ToList();

            // Add new
            if (inputsToAdd.Any()) await _targetRepo.AddWorkflowTargets(inputsToAdd);

            // Update existing
            if (inputsToUpdate.Any()) await _targetRepo.UpdateWorkflowTargets(inputsToUpdate);

            // Delete removed
            if (inputsToDelete.Any()) await _targetRepo.DeleteWorkflowTargets(inputsToDelete.Select(i => i.Workflow_Target_Id).ToList());
        }

        /// <summary>
        /// Handles the logic of determining which modifiers need to be created, updated, or deleted, and performs those operations.
        /// Calls repos to perform DB operations.
        /// </summary>
        private async Task UpdateModifiers(List<WorkflowNodeModifier> newModifiers, List<WorkflowNodeModifier> originalModifiers)
        {
            // Use dictionaries for O(1) lookups
            var originalDict = originalModifiers.ToDictionary(t => t.Workflow_Node_Modifier_Id);
            // Limit newDict to only modifiers that have an assigned Workflow_Node_Modifier_Id to avoid issues with new modifiers having default 0 value
            var newDict = newModifiers.Where(t => t.Workflow_Node_Modifier_Id != 0).ToDictionary(t => t.Workflow_Node_Modifier_Id);
            
            // Add: in new, not in original
            var inputsToAdd = newModifiers.Where(nt => !originalDict.ContainsKey(nt.Workflow_Node_Modifier_Id)).ToList();
            // Update: in both, but values differ
            var inputsToUpdate = newModifiers.Where(nt => originalDict.TryGetValue(nt.Workflow_Node_Modifier_Id, out var ot) && !ot.ValueEquals(nt)).ToList();
            // Delete: in original, not in new
            var inputsToDelete = originalDict.Values.Where(et => !newDict.ContainsKey(et.Workflow_Node_Modifier_Id)).ToList();

            // Add new
            if (inputsToAdd.Any()) await _modifierRepo.AddWorkflowNodeModifiers(inputsToAdd);

            // Update existing
            if (inputsToUpdate.Any()) await _modifierRepo.UpdateWorkflowNodeModifiers(inputsToUpdate);

            // Delete removed
            if (inputsToDelete.Any()) await _modifierRepo.DeleteWorkflowNodeModifiers(inputsToDelete.Select(i => i.Workflow_Node_Modifier_Id).ToList());
        }

        /// <summary>
        /// Ensures that all attributes in the NodeChart have their Workflow_Node_Id set to the Node_Id of the node they belong to
        /// Ensures ModifierAttributes also have their Workflow_Node_Modifier_Id set to the correct modifier.
        /// This is required since Workflow_Node_Id and Workflow_Node_Modifier_Id are assigned by the database.
        /// </summary>
        private void NormalizeAttributeReferences(NodeChart nodeChart)
        {
            foreach (var node in nodeChart.Nodes)
            {
                foreach (var recipeAttribute in node.RecipeAttributes)
                {
                    recipeAttribute.Workflow_Node_Id = node.Node.Node_Id;
                }

                foreach (var machineAttribute in node.MachineAttributes)
                {
                    machineAttribute.Workflow_Node_Id = node.Node.Node_Id;
                }

                foreach (var fullModifier in node.Modifiers)
                {
                    foreach (var modifierAttribute in fullModifier.ModifierAttributes)
                    {
                        modifierAttribute.Workflow_Node_Id = node.Node.Node_Id;
                        modifierAttribute.Workflow_Node_Modifier_Id = fullModifier.Modifier.Workflow_Node_Modifier_Id;
                        modifierAttribute.Modifier_Id = fullModifier.Modifier.Modifier_Id;
                    }
                }
            }
        }

        private async Task UpdateRecipeAttributes(List<WorkflowRecipeAttribute> newAttributes, List<WorkflowRecipeAttribute> originalAttributes)
        {
            var originalDict = originalAttributes.ToDictionary(t => t.Workflow_Recipe_Attribute_Id);
            var newDict = newAttributes.Where(t => t.Workflow_Recipe_Attribute_Id != 0).ToDictionary(t => t.Workflow_Recipe_Attribute_Id);

            var inputsToAdd = newAttributes.Where(nt => !originalDict.ContainsKey(nt.Workflow_Recipe_Attribute_Id)).ToList();
            var inputsToUpdate = newAttributes.Where(nt => originalDict.TryGetValue(nt.Workflow_Recipe_Attribute_Id, out var ot) && !ot.ValueEquals(nt)).ToList();
            var inputsToDelete = originalDict.Values.Where(et => !newDict.ContainsKey(et.Workflow_Recipe_Attribute_Id)).ToList();

            if (inputsToAdd.Any()) await _recipeAttributeRepo.AddWorkflowRecipeAttributes(inputsToAdd);
            if (inputsToUpdate.Any()) await _recipeAttributeRepo.UpdateWorkflowRecipeAttributes(inputsToUpdate);
            if (inputsToDelete.Any()) await _recipeAttributeRepo.DeleteWorkflowRecipeAttributes(inputsToDelete.Select(i => i.Workflow_Recipe_Attribute_Id).ToList());
        }

        private async Task UpdateMachineAttributes(List<WorkflowMachineAttribute> newAttributes, List<WorkflowMachineAttribute> originalAttributes)
        {
            var originalDict = originalAttributes.ToDictionary(t => t.Workflow_Machine_Attribute_Id);
            var newDict = newAttributes.Where(t => t.Workflow_Machine_Attribute_Id != 0).ToDictionary(t => t.Workflow_Machine_Attribute_Id);

            var inputsToAdd = newAttributes.Where(nt => !originalDict.ContainsKey(nt.Workflow_Machine_Attribute_Id)).ToList();
            var inputsToUpdate = newAttributes.Where(nt => originalDict.TryGetValue(nt.Workflow_Machine_Attribute_Id, out var ot) && !ot.ValueEquals(nt)).ToList();
            var inputsToDelete = originalDict.Values.Where(et => !newDict.ContainsKey(et.Workflow_Machine_Attribute_Id)).ToList();

            if (inputsToAdd.Any()) await _machineAttributeRepo.AddWorkflowMachineAttributes(inputsToAdd);
            if (inputsToUpdate.Any()) await _machineAttributeRepo.UpdateWorkflowMachineAttributes(inputsToUpdate);
            if (inputsToDelete.Any()) await _machineAttributeRepo.DeleteWorkflowMachineAttributes(inputsToDelete.Select(i => i.Workflow_Machine_Attribute_Id).ToList());
        }

        private async Task UpdateModifierAttributes(List<WorkflowModifierAttribute> newAttributes, List<WorkflowModifierAttribute> originalAttributes)
        {
            var originalDict = originalAttributes.ToDictionary(t => t.Workflow_Modifier_Attribute_Id);
            var newDict = newAttributes.Where(t => t.Workflow_Modifier_Attribute_Id != 0).ToDictionary(t => t.Workflow_Modifier_Attribute_Id);

            var inputsToAdd = newAttributes.Where(nt => !originalDict.ContainsKey(nt.Workflow_Modifier_Attribute_Id)).ToList();
            var inputsToUpdate = newAttributes.Where(nt => originalDict.TryGetValue(nt.Workflow_Modifier_Attribute_Id, out var ot) && !ot.ValueEquals(nt)).ToList();
            var inputsToDelete = originalDict.Values.Where(et => !newDict.ContainsKey(et.Workflow_Modifier_Attribute_Id)).ToList();

            if (inputsToAdd.Any()) await _workflowModifierAttributeRepo.AddWorkflowModifierAttributes(inputsToAdd);
            if (inputsToUpdate.Any()) await _workflowModifierAttributeRepo.UpdateWorkflowModifierAttributes(inputsToUpdate);
            if (inputsToDelete.Any()) await _workflowModifierAttributeRepo.DeleteWorkflowModifierAttributes(inputsToDelete.Select(i => i.Workflow_Modifier_Attribute_Id).ToList());
        }

        /// <summary>
        /// Handles the logic of determining which recipes need to be created, updated, or deleted, and performs those operations.
        /// Calls repos to perform DB operations.
        /// </summary>
        private async Task UpdateRecipes(List<WorkflowRecipe> newRecipes, List<WorkflowRecipe> originalRecipes)
        {
            // Use dictionaries for O(1) lookups
            var originalDict = originalRecipes.ToDictionary(t => t.Workflow_Recipe_Id);
            // Limit newDict to only recipes that have an assigned Workflow_Recipe_Id to avoid issues with new recipes having default 0 value
            var newDict = newRecipes.Where(t => t.Workflow_Recipe_Id != 0).ToDictionary(t => t.Workflow_Recipe_Id);
            
            // Add: in new, not in original
            var inputsToAdd = newRecipes.Where(nt => !originalDict.ContainsKey(nt.Workflow_Recipe_Id)).ToList();
            // Update: in both, but values differ
            var inputsToUpdate = newRecipes.Where(nt => originalDict.TryGetValue(nt.Workflow_Recipe_Id, out var ot) && !ot.ValueEquals(nt)).ToList();
            // Delete: in original, not in new
            var inputsToDelete = originalDict.Values.Where(et => !newDict.ContainsKey(et.Workflow_Recipe_Id)).ToList();

            // Add new
            if (inputsToAdd.Any()) await _recipeRepo.AddWorkflowRecipes(inputsToAdd);

            // Update existing
            if (inputsToUpdate.Any()) await _recipeRepo.UpdateWorkflowRecipes(inputsToUpdate);

            // Delete removed
            if (inputsToDelete.Any()) await _recipeRepo.DeleteWorkflowRecipes(inputsToDelete.Select(i => i.Workflow_Recipe_Id).ToList());
        }

        /// <summary>
        /// Handles the logic of determining which modifiers need to be created, updated, or deleted, and performs those operations.
        /// Calls repos to perform DB operations.
        /// </summary>
        private async Task UpdateEdges(List<WorkflowEdge> newEdges, List<WorkflowEdge> originalEdges)
        {
            // Use dictionaries for O(1) lookups
            var originalDict = originalEdges.ToDictionary(t => t.Workflow_Edge_Id);
            // Limit newDict to only edges that have an assigned Workflow_Edge_Id to avoid issues with new edges having default 0 value
            var newDict = newEdges.Where(t => t.Workflow_Edge_Id != 0).ToDictionary(t => t.Workflow_Edge_Id);
            // Add: in new, not in original
            var inputsToAdd = newEdges.Where(nt => !originalDict.ContainsKey(nt.Workflow_Edge_Id)).ToList();
            // Update: in both, but values differ
            var inputsToUpdate = newEdges.Where(nt => originalDict.TryGetValue(nt.Workflow_Edge_Id, out var ot) && !ot.ValueEquals(nt)).ToList();
            // Delete: in original, not in new
            var inputsToDelete = originalDict.Values.Where(et => !newDict.ContainsKey(et.Workflow_Edge_Id)).ToList();

            // Add new
            if (inputsToAdd.Any()) await _edgeRepo.AddWorkflowEdges(inputsToAdd);

            // Update existing
            if (inputsToUpdate.Any()) await _edgeRepo.UpdateWorkflowEdges(inputsToUpdate);

            // Delete removed
            if (inputsToDelete.Any()) await _edgeRepo.DeleteWorkflowEdges(inputsToDelete.Select(i => i.Workflow_Edge_Id).ToList());
        }

        /// <summary>
        /// Handles the logic of determining which modifiers need to be created, updated, or deleted, and performs those operations.
        /// Calls repos to perform DB operations.
        /// </summary>
        private async Task UpdateProductNodes(List<WorkflowProductNode> newProductNodes, List<WorkflowProductNode> originalProductNodes)
        {
            // Use dictionaries for O(1) lookups
            var originalDict = originalProductNodes.ToDictionary(t => t.Workflow_Product_Node_Id);
            // Limit newDict to only product nodes that have an assigned Workflow_Product_Node_Id to avoid issues with new product nodes having default 0 value
            var newDict = newProductNodes.Where(t => t.Workflow_Product_Node_Id != 0).ToDictionary(t => t.Workflow_Product_Node_Id);
            // Add: in new, not in original
            var inputsToAdd = newProductNodes.Where(nt => !originalDict.ContainsKey(nt.Workflow_Product_Node_Id)).ToList();
            // Update: in both, but values differ
            var inputsToUpdate = newProductNodes.Where(nt => originalDict.TryGetValue(nt.Workflow_Product_Node_Id, out var ot) && !ot.ValueEquals(nt)).ToList();
            // Delete: in original, not in new
            var inputsToDelete = originalDict.Values.Where(et => !newDict.ContainsKey(et.Workflow_Product_Node_Id)).ToList();

            // Add new
            if (inputsToAdd.Any()) await _productNodeRepo.AddWorkflowProductNodes(inputsToAdd);

            // Update existing
            if (inputsToUpdate.Any()) await _productNodeRepo.UpdateWorkflowProductNodes(inputsToUpdate);

            // Delete removed
            if (inputsToDelete.Any()) await _productNodeRepo.DeleteWorkflowProductNodes(inputsToDelete.Select(i => i.Workflow_Product_Node_Id).ToList());
        }

        /// <summary>
        /// Modifiers depend on nodes being assigned an ID by the database.
        /// Assigns the correct Workflow_Node_Id to each modifier based on the node it belongs to.
        /// </summary>
        private void SetNodeIdDependencies(NodeChart nodeChart)
        {
            foreach (var node in nodeChart.Nodes)
            {
                foreach (var recipeAttribute in node.RecipeAttributes)
                {
                    recipeAttribute.Workflow_Node_Id = node.Node.Node_Id;
                }
                foreach (var machineAttribute in node.MachineAttributes)
                {
                    machineAttribute.Workflow_Node_Id = node.Node.Node_Id;
                }
                foreach (var modifier in node.Modifiers)
                {
                    modifier.Modifier.Workflow_Node_Id = node.Node.Node_Id;
                }
            }
        }

        /// <summary>
        /// ModifiersAttributes depend on nodes and modifiers being assigned an ID by the database.
        /// Assigns the correct Workflow_Node_Id and Workflow_Node_Modifier_Id to each modifier attribute based on the modifier it belongs to.
        /// </summary>
        private void SetModifierIdsDependencies(NodeChart nodeChart)
        {
            foreach (var node in nodeChart.Nodes)
            {
                foreach (var modifier in node.Modifiers)
                {
                    foreach (var modifierAttribute in modifier.ModifierAttributes)
                    {
                        modifierAttribute.Workflow_Node_Id = node.Node.Node_Id;
                        modifierAttribute.Workflow_Node_Modifier_Id = modifier.Modifier.Workflow_Node_Modifier_Id;
                    }
                }
            }
        }
    }
}
