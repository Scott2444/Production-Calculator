using System.Diagnostics.CodeAnalysis;
using FakeItEasy;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Services;

namespace ProductionCalculator.Business.Tests.Services
{
    [ExcludeFromCodeCoverage]
    public class WorkflowChartDataServiceTests
    {
        private readonly IWorkflowNodeRepository _nodeRepo;
        private readonly IWorkflowTargetRepository _targetRepo;
        private readonly IWorkflowNodeModifierRepository _modifierRepo;
        private readonly IWorkflowEdgeRepository _edgeRepo;
        private readonly IWorkflowProductNodeRepository _productNodeRepo;
        private readonly IWorkflowRecipeRepository _recipeRepo;
        private readonly WorkflowNodeDbService _service;

        public WorkflowChartDataServiceTests()
        {
            _nodeRepo = A.Fake<IWorkflowNodeRepository>();
            _targetRepo = A.Fake<IWorkflowTargetRepository>();
            _modifierRepo = A.Fake<IWorkflowNodeModifierRepository>();
            _edgeRepo = A.Fake<IWorkflowEdgeRepository>();
            _productNodeRepo = A.Fake<IWorkflowProductNodeRepository>();
            _recipeRepo = A.Fake<IWorkflowRecipeRepository>();

            _service = new WorkflowNodeDbService(
                _nodeRepo,
                _targetRepo,
                _modifierRepo,
                _edgeRepo,
                _productNodeRepo,
                _recipeRepo
            );
        }

        [Fact]
        public async Task GetByWorkflowId_ShouldReturnAssembledNodeChart()
        {
            int workflowId = 1;
            var workflowNodes = new List<WorkflowNode>
            {
                new WorkflowNode { Node_Id = 10, Workflow_Id = 1, Puid = "node1", Recipe_Id = 1, Recipe_Version = 1 }
            };
            var modifiers = new List<WorkflowNodeModifier>
            {
                new WorkflowNodeModifier { Workflow_Node_Modifier_Id = 20, Workflow_Node_Id = 10, Modifier_Id = 1, Modifier_Version = 1 }
            };

            A.CallTo(() => _nodeRepo.GetByWorkflow(workflowId, false)).Returns(workflowNodes);
            A.CallTo(() => _modifierRepo.GetByNodeId(10, false)).Returns(modifiers);
            A.CallTo(() => _edgeRepo.GetByWorkflow(workflowId, false)).Returns(new List<WorkflowEdge>());
            A.CallTo(() => _targetRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowTarget>());
            A.CallTo(() => _productNodeRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowProductNode>());
            A.CallTo(() => _recipeRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowRecipe>());

            var result = await _service.GetByWorkflowId(workflowId);

            Assert.NotNull(result);
            Assert.Single(result.Nodes);
            Assert.Equal(10, result.Nodes[0].Node.Node_Id);
            Assert.Single(result.Nodes[0].Modifiers);
            Assert.Equal(20, result.Nodes[0].Modifiers[0].Workflow_Node_Modifier_Id);
        }

        [Fact]
        public async Task WorkflowUpdate_ShouldHandleNewNodesAndAssignIdsToDependencies()
        {
            int workflowId = 1;

            A.CallTo(() => _nodeRepo.GetByWorkflow(workflowId, false)).Returns(new List<WorkflowNode>());
            A.CallTo(() => _targetRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowTarget>());
            A.CallTo(() => _edgeRepo.GetByWorkflow(workflowId, false)).Returns(new List<WorkflowEdge>());
            A.CallTo(() => _productNodeRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowProductNode>());
            A.CallTo(() => _recipeRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowRecipe>());

            var newNode = new WorkflowNode { Node_Id = 0, Workflow_Id = workflowId, Puid = "new-node", Recipe_Id = 1, Recipe_Version = 1 };
            var newModifier = new WorkflowNodeModifier { Workflow_Node_Modifier_Id = 0, Workflow_Node_Id = 0, Modifier_Id = 2, Modifier_Version = 1 };

            var fullNode = new FullNode
            {
                Node = newNode,
                Modifiers = new List<WorkflowNodeModifier> { newModifier }
            };

            var nodeChart = new NodeChart
            {
                Nodes = new List<FullNode> { fullNode },
                Targets = new List<WorkflowTarget>(),
                Edges = new List<WorkflowEdge>(),
                ProductNodes = new List<WorkflowProductNode>(),
                PreferredRecipes = new List<WorkflowRecipe>()
            };

            A.CallTo(() => _nodeRepo.AddWorkflowNodes(A<List<WorkflowNode>>._))
                .Invokes((List<WorkflowNode> nodes) =>
                {
                    foreach (var n in nodes)
                    {
                        n.Node_Id = 100;
                    }
                });

            A.CallTo(() => _modifierRepo.AddWorkflowNodeModifiers(A<List<WorkflowNodeModifier>>._))
                .Invokes((List<WorkflowNodeModifier> mods) =>
                {
                    foreach (var m in mods)
                    {
                        m.Workflow_Node_Modifier_Id = 200;
                    }
                });

            await _service.WorkflowUpdate(workflowId, nodeChart);

            Assert.Equal(100, newNode.Node_Id);
            Assert.Equal(100, newModifier.Workflow_Node_Id);
            Assert.Equal(200, newModifier.Workflow_Node_Modifier_Id);

            A.CallTo(() => _nodeRepo.AddWorkflowNodes(A<List<WorkflowNode>>.That.Contains(newNode))).MustHaveHappenedOnceExactly();
            A.CallTo(() => _modifierRepo.AddWorkflowNodeModifiers(A<List<WorkflowNodeModifier>>.That.Contains(newModifier))).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WorkflowUpdate_ShouldUpdateExistingNodesAndModifiers()
        {
            int workflowId = 1;
            var originalNode = new WorkflowNode { Node_Id = 10, Workflow_Id = workflowId, Puid = "node1", Recipe_Id = 1, Recipe_Version = 1 };
            var originalModifier = new WorkflowNodeModifier { Workflow_Node_Modifier_Id = 40, Workflow_Node_Id = 10, Modifier_Id = 1, Modifier_Version = 1 };

            A.CallTo(() => _nodeRepo.GetByWorkflow(workflowId, false)).Returns(new List<WorkflowNode> { originalNode });
            A.CallTo(() => _modifierRepo.GetByNodeId(10, false)).Returns(new List<WorkflowNodeModifier> { originalModifier });
            A.CallTo(() => _targetRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowTarget>());
            A.CallTo(() => _edgeRepo.GetByWorkflow(workflowId, false)).Returns(new List<WorkflowEdge>());
            A.CallTo(() => _productNodeRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowProductNode>());
            A.CallTo(() => _recipeRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowRecipe>());

            var updatedNode = new WorkflowNode { Node_Id = 10, Workflow_Id = workflowId, Puid = "node1", Recipe_Id = 1, Recipe_Version = 2 };
            var updatedModifier = new WorkflowNodeModifier { Workflow_Node_Modifier_Id = 40, Workflow_Node_Id = 10, Modifier_Id = 2, Modifier_Version = 1 };

            var nodeChart = new NodeChart
            {
                Nodes = new List<FullNode>
                {
                    new FullNode
                    {
                        Node = updatedNode,
                        Modifiers = new List<WorkflowNodeModifier> { updatedModifier }
                    }
                },
                Targets = new List<WorkflowTarget>(),
                Edges = new List<WorkflowEdge>(),
                ProductNodes = new List<WorkflowProductNode>(),
                PreferredRecipes = new List<WorkflowRecipe>()
            };

            await _service.WorkflowUpdate(workflowId, nodeChart);

            A.CallTo(() => _nodeRepo.UpdateWorkflowNodes(A<List<WorkflowNode>>.That.Matches(l => l.Any(n => n.Recipe_Version == 2)))).MustHaveHappenedOnceExactly();
            A.CallTo(() => _modifierRepo.UpdateWorkflowNodeModifiers(A<List<WorkflowNodeModifier>>.That.Matches(l => l.Any(m => m.Modifier_Id == 2)))).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WorkflowUpdate_ShouldDeleteRemovedItems()
        {
            int workflowId = 1;
            var originalNode = new WorkflowNode { Node_Id = 10, Workflow_Id = workflowId, Puid = "node1", Recipe_Id = 1, Recipe_Version = 1 };

            A.CallTo(() => _nodeRepo.GetByWorkflow(workflowId, false)).Returns(new List<WorkflowNode> { originalNode });
            A.CallTo(() => _modifierRepo.GetByNodeId(10, false)).Returns(new List<WorkflowNodeModifier>());
            A.CallTo(() => _targetRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowTarget>());
            A.CallTo(() => _edgeRepo.GetByWorkflow(workflowId, false)).Returns(new List<WorkflowEdge>());
            A.CallTo(() => _productNodeRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowProductNode>());
            A.CallTo(() => _recipeRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowRecipe>());

            var nodeChart = new NodeChart
            {
                Nodes = new List<FullNode>(),
                Targets = new List<WorkflowTarget>(),
                Edges = new List<WorkflowEdge>(),
                ProductNodes = new List<WorkflowProductNode>(),
                PreferredRecipes = new List<WorkflowRecipe>()
            };

            await _service.WorkflowUpdate(workflowId, nodeChart);

            A.CallTo(() => _nodeRepo.DeleteWorkflowNodes(A<List<int>>.That.Contains(10))).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WorkflowEdgeUpdate_ShouldUpdateEdges()
        {
            int workflowId = 1;
            var originalEdge = new WorkflowEdge { Workflow_Edge_Id = 500, Workflow_Id = workflowId, Producer_Node_Id = 1, Consumer_Node_Id = 2, Product_Node_Id = 3, Calculated_Flow_Rate = 0, Actual_Flow_Rate = 0 };

            A.CallTo(() => _nodeRepo.GetByWorkflow(workflowId, false)).Returns(new List<WorkflowNode>());
            A.CallTo(() => _edgeRepo.GetByWorkflow(workflowId, false)).Returns(new List<WorkflowEdge> { originalEdge });
            A.CallTo(() => _targetRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowTarget>());
            A.CallTo(() => _productNodeRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowProductNode>());
            A.CallTo(() => _recipeRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowRecipe>());

            var newEdge = new WorkflowEdge { Workflow_Edge_Id = 0, Workflow_Id = workflowId, Producer_Node_Id = 2, Consumer_Node_Id = 3, Product_Node_Id = 3, Calculated_Flow_Rate = 0, Actual_Flow_Rate = 1.0 };
            var nodeChart = new NodeChart
            {
                Nodes = new List<FullNode>(),
                Edges = new List<WorkflowEdge> { newEdge },
                Targets = new List<WorkflowTarget>(),
                ProductNodes = new List<WorkflowProductNode>(),
                PreferredRecipes = new List<WorkflowRecipe>()
            };

            await _service.WorkflowEdgeUpdate(workflowId, nodeChart);

            A.CallTo(() => _edgeRepo.DeleteWorkflowEdges(A<List<int>>.That.Contains(500))).MustHaveHappenedOnceExactly();
            A.CallTo(() => _edgeRepo.AddWorkflowEdges(A<List<WorkflowEdge>>.That.Contains(newEdge))).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task NodePuidExists_ShouldReturnRepoResult()
        {
            string puid = "some-puid";
            A.CallTo(() => _nodeRepo.PuidExists(puid)).Returns(true);

            var result = await _service.NodePuidExists(puid);

            Assert.True(result);
            A.CallTo(() => _nodeRepo.PuidExists(puid)).MustHaveHappenedOnceExactly();
        }
    }
}
