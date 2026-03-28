using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Services
{
	public class WorkflowNodeDbService : IWorkflowChartDataService
	{
		private readonly IWorkflowNodeRepository _nodeRepo;
		private readonly IWorkflowTargetRepository _targetRepo;
        private readonly IWorkflowNodeModifierRepository _modifierRepo;
        private readonly IWorkflowEdgeRepository _edgeRepo;
        private readonly IWorkflowProductNodeRepository _productNodeRepo;
        private readonly IWorkflowRecipeRepository _recipeRepo;

		public WorkflowNodeDbService(
            IWorkflowNodeRepository nodeRepo, 
            IWorkflowTargetRepository targetRepo, 
            IWorkflowNodeModifierRepository modifierRepo, 
            IWorkflowEdgeRepository edgeRepo,
            IWorkflowProductNodeRepository productNodeRepo,
            IWorkflowRecipeRepository recipeRepo
            )
		{
			_nodeRepo = nodeRepo;
			_targetRepo = targetRepo;
			_modifierRepo = modifierRepo;
            _edgeRepo = edgeRepo;
            _productNodeRepo = productNodeRepo;
            _recipeRepo = recipeRepo;
		}

        /// <summary>
        /// Retrieves all nodes, edges, targets, and product nodes for a given workflow, along with their modifiers.
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
                nodes.Add(new FullNode
                {
                    Node = workflowNode,
                    Modifiers = modifiers,
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
        /// Modifiers depend on nodes and this will automatically assign those node_ids to the modifiers.
        /// </summary>
        /// <returns>Updated NodeChart with IDs assigned by the database</returns>
        public async Task<NodeChart> WorkflowUpdate(int workflowId, NodeChart nodeChart)
        {
            var originalChart = await GetByWorkflowId(workflowId, isTracked: false);
            
            await UpdateNodes(nodeChart.Nodes.Select(n => n.Node).ToList(), originalChart.Nodes.Select(n => n.Node).ToList());
            await UpdateTargets(nodeChart.Targets, originalChart.Targets);

            SetNodeIdDependencies(nodeChart); // Solves Node_Id dependency
            await UpdateModifiers(
                nodeChart.Nodes.SelectMany(n => n.Modifiers).ToList(),
                originalChart.Nodes.SelectMany(n => n.Modifiers).ToList());
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
                foreach (var modifier in node.Modifiers)
                {
                    modifier.Workflow_Node_Id = node.Node.Node_Id;
                }
            }
        }
    }
}
