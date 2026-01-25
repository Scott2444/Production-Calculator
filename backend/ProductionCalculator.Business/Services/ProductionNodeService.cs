
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Services
{
	public class ProductionNodeService : IProductionNodeService
	{
		private readonly IProductionNodeRepository _nodeRepo;
		private readonly IProductionNodeInputRepository _inputRepo;
        private readonly IProductionNodeModifierRepository _modifierRepo;
        private readonly IProductionNodeStateRepository _stateRepo;

		public ProductionNodeService(IProductionNodeRepository nodeRepo, IProductionNodeInputRepository inputRepo, IProductionNodeModifierRepository modifierRepo, IProductionNodeStateRepository stateRepo)
		{
			_nodeRepo = nodeRepo;
			_inputRepo = inputRepo;
			_modifierRepo = modifierRepo;
            _stateRepo = stateRepo;
		}
        public async Task<IEnumerable<FullProductionNode>> GetByWorkflowId(int workflowId, bool isTracked = true)
        {
            var nodes = await _nodeRepo.GetByWorkflowId(workflowId, isTracked);
            var fullNodes = new List<FullProductionNode>();

            foreach (var node in nodes)
            {
                var nodeInputs = await _inputRepo.GetByNodeId(node.Node_Id, isTracked);
                var nodeModifiers = await _modifierRepo.GetByNodeId(node.Node_Id, isTracked);
                var nodeState = await _stateRepo.GetByNodeId(node.Node_Id, isTracked);

                nodeState ??= new ProductionNodeState  // Default state if none exists
                    {
                        Node_Id = node.Node_Id,
                        Actual_Machine_Count = 0,
                        External_Supply_Rate = null,
                        Realized_Recipe_Rate = 0
                    };

                var fullNode = new FullProductionNode
                {
                    Node_Id = node.Node_Id,
                    Workflow_Id = node.Workflow_Id,
                    Puid = node.Puid,
                    Product_Id = node.Product_Id,
                    Product_Version = node.Product_Version,
                    Recipe_Id = node.Recipe_Id,
                    Recipe_Version = node.Recipe_Version,
                    Machine_Id = node.Machine_Id,
                    Machine_Version = node.Machine_Version,
                    Parent_Node_Id = node.Parent_Node_Id,
                    Target_Rate = node.Target_Rate,
                    Ideal_Machine_Count = node.Ideal_Machine_Count,
                    Is_Root = node.Is_Root,
                    Is_External = node.Is_External,
                    Created_At = node.Created_At,
                    Last_Updated = node.Last_Updated,
                    Inputs = nodeInputs,
                    Modifiers = nodeModifiers,
                    State = nodeState
                };

                fullNodes.Add(fullNode);
            }

            return fullNodes;
        }
        /// <summary>
        /// Handles the logic of determining which nodes need to be created, updated, or deleted, and performs those operations.
        /// Translates FullProductionNode to subcomponent and calls respective repos.
        /// </summary>
        public async Task CompleteUpdateProductionNodes(int workflowId, IEnumerable<FullProductionNode> productionNodes)
        {
            // Get original nodes from DB
            var originalNodes = await GetByWorkflowId(workflowId, isTracked: false);

            // Nodes must be handled first to ensure foreign key constraints are met
            await HandleNodes(originalNodes.Select(MapToProductionNode), productionNodes.Select(MapToProductionNode));

            await HandleNodeInputs(
                originalNodes.SelectMany(n => n.Inputs),
                productionNodes.SelectMany(n => n.Inputs));

            await HandleNodeModifiers(
                originalNodes.SelectMany(n => n.Modifiers),
                productionNodes.SelectMany(n => n.Modifiers));

            await HandleNodeState(
                originalNodes.Select(n => n.State),
                productionNodes.Select(n => n.State));
        }

        private async Task HandleNodeInputs(IEnumerable<ProductionNodeInput> originalInputs, IEnumerable<ProductionNodeInput> newInputs)
        {
            // Use dictionaries for O(1) lookups
            var originalDict = originalInputs.ToDictionary(i => i.Node_Input_Id);
            var newDict = newInputs.ToDictionary(i => i.Node_Input_Id);
            // Add: in new, not in original
            var inputsToAdd = newDict.Values.Where(ni => !originalDict.ContainsKey(ni.Node_Input_Id)).ToList();
            // Update: in both, but values differ
            var inputsToUpdate = newDict.Values.Where(ni => originalDict.TryGetValue(ni.Node_Input_Id, out var oi) && !oi.ValueEquals(ni)).ToList();
            // Delete: in original, not in new
            var inputsToDelete = originalDict.Values.Where(ei => !newDict.ContainsKey(ei.Node_Input_Id)).ToList();

            // Add new inputs
            await _inputRepo.AddProductionNodeInputs(inputsToAdd);

            // Update existing inputs
            await _inputRepo.UpdateProductionNodeInputs(inputsToUpdate);

            // Delete removed inputs
            await _inputRepo.DeleteProductionNodeInputs(inputsToDelete.Select(i => i.Node_Input_Id).ToList());
        }

        private async Task HandleNodeModifiers(IEnumerable<ProductionNodeModifier> originalModifiers, IEnumerable<ProductionNodeModifier> newModifiers)
        {
            // Use dictionaries for O(1) lookups
            var originalDict = originalModifiers.ToDictionary(m => m.Node_Modifier_Id);
            var newDict = newModifiers.ToDictionary(m => m.Node_Modifier_Id);
            // Add: in new, not in original
            var modifiersToAdd = newDict.Values.Where(nm => !originalDict.ContainsKey(nm.Node_Modifier_Id)).ToList();
            // Update: in both, but values differ
            var modifiersToUpdate = newDict.Values.Where(nm => originalDict.TryGetValue(nm.Node_Modifier_Id, out var om) && !om.ValueEquals(nm)).ToList();
            // Delete: in original, not in new
            var modifiersToDelete = originalDict.Values.Where(em => !newDict.ContainsKey(em.Node_Modifier_Id)).ToList();

            // Add new modifiers
            await _modifierRepo.AddProductionNodeModifiers(modifiersToAdd);

            // Update existing modifiers
            await _modifierRepo.UpdateProductionNodeModifiers(modifiersToUpdate);

            // Delete removed modifiers
            await _modifierRepo.DeleteProductionNodeModifiers(modifiersToDelete.Select(m => m.Node_Modifier_Id).ToList());
        }

        private async Task HandleNodeState(IEnumerable<ProductionNodeState> originalStates, IEnumerable<ProductionNodeState> newStates)
        {
            // Use dictionaries for O(1) lookups
            var originalDict = originalStates.ToDictionary(s => s.Node_Id);
            var newDict = newStates.ToDictionary(s => s.Node_Id);
            // Add: in new, not in original
            var statesToAdd = newDict.Values.Where(ns => !originalDict.ContainsKey(ns.Node_Id)).ToList();
            // Update: in both, but values differ
            var statesToUpdate = newDict.Values.Where(ns => originalDict.TryGetValue(ns.Node_Id, out var os) && !os.ValueEquals(ns)).ToList();
            // Delete: in original, not in new
            var statesToDelete = originalDict.Values.Where(os => !newDict.ContainsKey(os.Node_Id)).ToList();

            // Add new states
            await _stateRepo.AddProductionNodeStates(statesToAdd);

            // Update existing states
            await _stateRepo.UpdateProductionNodeStates(statesToUpdate);

            // Delete removed states
            await _stateRepo.DeleteProductionNodeStates(statesToDelete.Select(s => s.Node_Id).ToList());
        }

        private async Task HandleNodes(IEnumerable<ProductionNode> originalNodes, IEnumerable<ProductionNode> newNodes)
        {
            // Use dictionaries for O(1) lookups
            var originalDict = originalNodes.ToDictionary(n => n.Node_Id);
            var newDict = newNodes.ToDictionary(n => n.Node_Id);
            // Add: in new, not in original
            var nodesToAdd = newDict.Values.Where(nn => !originalDict.ContainsKey(nn.Node_Id)).ToList();
            // Update: in both, but values differ
            var nodesToUpdate = newDict.Values.Where(nn => originalDict.TryGetValue(nn.Node_Id, out var on) && !on.ValueEquals(nn)).ToList();
            // Delete: in original, not in new
            var nodesToDelete = originalDict.Values.Where(on => !newDict.ContainsKey(on.Node_Id)).ToList();

            // Add new nodes
            await _nodeRepo.AddProductionNodes(nodesToAdd);

            // Update existing nodes
            await _nodeRepo.UpdateProductionNodes(nodesToUpdate);

            // Delete removed nodes
            await _nodeRepo.DeleteProductionNodes(nodesToDelete.Select(n => n.Node_Id).ToList());
        }

        private ProductionNode MapToProductionNode(FullProductionNode fullNode)
        {
            return new ProductionNode
            {
                Node_Id = fullNode.Node_Id,
                Workflow_Id = fullNode.Workflow_Id,
                Puid = fullNode.Puid,
                Product_Id = fullNode.Product_Id,
                Product_Version = fullNode.Product_Version,
                Recipe_Id = fullNode.Recipe_Id,
                Recipe_Version = fullNode.Recipe_Version,
                Machine_Id = fullNode.Machine_Id,
                Machine_Version = fullNode.Machine_Version,
                Parent_Node_Id = fullNode.Parent_Node_Id,
                Target_Rate = fullNode.Target_Rate,
                Ideal_Machine_Count = fullNode.Ideal_Machine_Count,
                Is_Root = fullNode.Is_Root,
                Is_External = fullNode.Is_External,
                Created_At = fullNode.Created_At,
                Last_Updated = fullNode.Last_Updated
            };
        }
	}
}
