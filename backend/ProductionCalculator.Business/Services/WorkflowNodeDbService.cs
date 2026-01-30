using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Services
{
	public class WorkflowNodeDbService : IWorkflowNodeDbService
	{
		private readonly IWorkflowNodeRepository _nodeRepo;
		private readonly IWorkflowTargetRepository _targetRepo;
        private readonly IWorkflowNodeModifierRepository _modifierRepo;
        private readonly IWorkflowEdgeRepository _edgeRepo;

		public WorkflowNodeDbService(IWorkflowNodeRepository nodeRepo, IWorkflowTargetRepository targetRepo, IWorkflowNodeModifierRepository modifierRepo, IWorkflowEdgeRepository edgeRepo)
		{
			_nodeRepo = nodeRepo;
			_targetRepo = targetRepo;
			_modifierRepo = modifierRepo;
            _edgeRepo = edgeRepo;
		}
        public async Task<NodeChart> GetByWorkflowId(int workflowId, bool isTracked = false)
        {
            var nodes = new List<FullNode>();
            var workflowNodes = await _nodeRepo.GetByWorkflow(workflowId, isTracked);
            foreach (var workflowNode in workflowNodes)
            {
                nodes.Add(new FullNode
                {
                    Node = workflowNode,
                    Modifiers = await _modifierRepo.GetByNodeId(workflowNode.Node_Id, isTracked),
                });
            }
            var edges = await _edgeRepo.GetByWorkflow(workflowId, isTracked);
            var targets = await _targetRepo.GetByWorkflowId(workflowId, isTracked);
            return new NodeChart
            {
                Nodes = nodes,
                Edges = edges,
                Targets = targets
            };
        }
        
        /// <summary>
        /// Handles the logic of determining which nodes need to be created, updated, or deleted, and performs those operations.
        /// Translates NodeChart to subcomponent and calls respective repos.
        /// </summary>
        public async Task CompleteWorkflowUpdate(int workflowId, NodeChart nodeChart)
        {
            var originalChart = await GetByWorkflowId(workflowId, isTracked: true);
            
            await UpdateNodes(nodeChart.Nodes.Select(n => n.Node).ToList(), originalChart.Nodes.Select(n => n.Node).ToList());
            await UpdateTargets(nodeChart.Targets, originalChart.Targets);
            await UpdateModifiers(nodeChart.Nodes.SelectMany(n => n.Modifiers).ToList(), originalChart.Nodes.SelectMany(n => n.Modifiers).ToList());
            await UpdateEdges(nodeChart.Edges, originalChart.Edges);
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
            var newDict = newNodes.ToDictionary(n => n.Node_Id);
            // Add: in new, not in original
            var inputsToAdd = newDict.Values.Where(nn => !originalDict.ContainsKey(nn.Node_Id)).ToList();
            // Update: in both, but values differ
            var inputsToUpdate = newDict.Values.Where(nn => originalDict.TryGetValue(nn.Node_Id, out var on) && !on.ValueEquals(nn)).ToList();
            // Delete: in original, not in new
            var inputsToDelete = originalDict.Values.Where(en => !newDict.ContainsKey(en.Node_Id)).ToList();

            // Add new
            await _nodeRepo.AddWorkflowNodes(inputsToAdd);

            // Update existing
            await _nodeRepo.UpdateWorkflowNodes(inputsToUpdate);

            // Delete removed
            await _nodeRepo.DeleteWorkflowNodes(inputsToDelete.Select(i => i.Node_Id).ToList());
        }

        /// <summary>
        /// Handles the logic of determining which targets need to be created, updated, or deleted, and performs those operations.
        /// Calls repos to perform DB operations.
        /// </summary>
        private async Task UpdateTargets(List<WorkflowTarget> newTargets, List<WorkflowTarget> originalTargets)
        {
            // Use dictionaries for O(1) lookups
            var originalDict = originalTargets.ToDictionary(t => t.Workflow_Target_Id);
            var newDict = newTargets.ToDictionary(t => t.Workflow_Target_Id);
            // Add: in new, not in original
            var inputsToAdd = newDict.Values.Where(nt => !originalDict.ContainsKey(nt.Workflow_Target_Id)).ToList();
            // Update: in both, but values differ
            var inputsToUpdate = newDict.Values.Where(nt => originalDict.TryGetValue(nt.Workflow_Target_Id, out var ot) && !ot.ValueEquals(nt)).ToList();
            // Delete: in original, not in new
            var inputsToDelete = originalDict.Values.Where(et => !newDict.ContainsKey(et.Workflow_Target_Id)).ToList();

            // Add new
            await _targetRepo.AddWorkflowTargets(inputsToAdd);

            // Update existing
            await _targetRepo.UpdateWorkflowTargets(inputsToUpdate);

            // Delete removed
            await _targetRepo.DeleteWorkflowTargets(inputsToDelete.Select(i => i.Workflow_Target_Id).ToList());
        }

        /// <summary>
        /// Handles the logic of determining which modifiers need to be created, updated, or deleted, and performs those operations.
        /// Calls repos to perform DB operations.
        /// </summary>
        private async Task UpdateModifiers(List<WorkflowNodeModifier> newModifiers, List<WorkflowNodeModifier> originalModifiers)
        {
            // Use dictionaries for O(1) lookups
            var originalDict = originalModifiers.ToDictionary(t => t.Workflow_Node_Modifier_Id);
            var newDict = newModifiers.ToDictionary(t => t.Workflow_Node_Modifier_Id);
            // Add: in new, not in original
            var inputsToAdd = newDict.Values.Where(nt => !originalDict.ContainsKey(nt.Workflow_Node_Modifier_Id)).ToList();
            // Update: in both, but values differ
            var inputsToUpdate = newDict.Values.Where(nt => originalDict.TryGetValue(nt.Workflow_Node_Modifier_Id, out var ot) && !ot.ValueEquals(nt)).ToList();
            // Delete: in original, not in new
            var inputsToDelete = originalDict.Values.Where(et => !newDict.ContainsKey(et.Workflow_Node_Modifier_Id)).ToList();

            // Add new
            await _modifierRepo.AddWorkflowNodeModifiers(inputsToAdd);

            // Update existing
            await _modifierRepo.UpdateWorkflowNodeModifiers(inputsToUpdate);

            // Delete removed
            await _modifierRepo.DeleteWorkflowNodeModifiers(inputsToDelete.Select(i => i.Workflow_Node_Modifier_Id).ToList());
        }

        /// <summary>
        /// Handles the logic of determining which modifiers need to be created, updated, or deleted, and performs those operations.
        /// Calls repos to perform DB operations.
        /// </summary>
        private async Task UpdateEdges(List<WorkflowEdge> newEdges, List<WorkflowEdge> originalEdges)
        {
            // Use dictionaries for O(1) lookups
            var originalDict = originalEdges.ToDictionary(t => t.Workflow_Edge_Id);
            var newDict = newEdges.ToDictionary(t => t.Workflow_Edge_Id);
            // Add: in new, not in original
            var inputsToAdd = newDict.Values.Where(nt => !originalDict.ContainsKey(nt.Workflow_Edge_Id)).ToList();
            // Update: in both, but values differ
            var inputsToUpdate = newDict.Values.Where(nt => originalDict.TryGetValue(nt.Workflow_Edge_Id, out var ot) && !ot.ValueEquals(nt)).ToList();
            // Delete: in original, not in new
            var inputsToDelete = originalDict.Values.Where(et => !newDict.ContainsKey(et.Workflow_Edge_Id)).ToList();

            // Add new
            await _edgeRepo.AddWorkflowEdges(inputsToAdd);

            // Update existing
            await _edgeRepo.UpdateWorkflowEdges(inputsToUpdate);

            // Delete removed
            await _edgeRepo.DeleteWorkflowEdges(inputsToDelete.Select(i => i.Workflow_Edge_Id).ToList());
        }

        
	}
}
