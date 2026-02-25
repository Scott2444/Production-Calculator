using FakeItEasy;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Services;

namespace ProductionCalculator.Business.Tests.Services
{
    public class WorkflowChartDataServiceTests
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
        private readonly WorkflowNodeDbService _service;

        public WorkflowChartDataServiceTests()
        {
            _nodeRepo = A.Fake<IWorkflowNodeRepository>();
            _targetRepo = A.Fake<IWorkflowTargetRepository>();
            _modifierRepo = A.Fake<IWorkflowNodeModifierRepository>();
            _recipeAttributeRepo = A.Fake<IWorkflowRecipeAttributeRepository>();
            _machineAttributeRepo = A.Fake<IWorkflowMachineAttributeRepository>();
            _workflowModifierAttributeRepo = A.Fake<IWorkflowModifierAttributeRepository>();
            _edgeRepo = A.Fake<IWorkflowEdgeRepository>();
            _productNodeRepo = A.Fake<IWorkflowProductNodeRepository>();
            _recipeRepo = A.Fake<IWorkflowRecipeRepository>();

            _service = new WorkflowNodeDbService(
                _nodeRepo,
                _targetRepo,
                _modifierRepo,
                _recipeAttributeRepo,
                _machineAttributeRepo,
                _workflowModifierAttributeRepo,
                _edgeRepo,
                _productNodeRepo,
                _recipeRepo
            );
        }

        [Fact]
        public async Task GetByWorkflowId_ShouldReturnAssembledNodeChart()
        {
            // Arrange
            int workflowId = 1;
            var workflowNodes = new List<WorkflowNode>
            {
                new WorkflowNode { Node_Id = 10, Workflow_Id = 1, Puid = "node1", Recipe_Id = 1, Recipe_Version = 1 }
            };
            var modifiers = new List<WorkflowNodeModifier>
            {
                new WorkflowNodeModifier { Workflow_Node_Modifier_Id = 20, Workflow_Node_Id = 10, Modifier_Id = 1, Modifier_Version = 1 }
            };
            var modifierAttributes = new List<WorkflowModifierAttribute>
            {
                new WorkflowModifierAttribute { Workflow_Modifier_Attribute_Id = 30, Workflow_Node_Id = 10, Workflow_Node_Modifier_Id = 20, Modifier_Id = 1, Attribute_Id = 1, Flat_Bonus = 0, Percent_Bonus = 0, Multiplicative_Bonus = 1 }
            };
            var recipeAttributes = new List<WorkflowRecipeAttribute>
            {
                new WorkflowRecipeAttribute { Workflow_Recipe_Attribute_Id = 40, Workflow_Node_Id = 10, Attribute_Id = 1, Rate = 1.0 }
            };
            var machineAttributes = new List<WorkflowMachineAttribute>
            {
                new WorkflowMachineAttribute { Workflow_Machine_Attribute_Id = 50, Workflow_Node_Id = 10, Attribute_Id = 1, Rate = 1.0 }
            };

            A.CallTo(() => _nodeRepo.GetByWorkflow(workflowId, false)).Returns(workflowNodes);
            A.CallTo(() => _modifierRepo.GetByNodeId(10, false)).Returns(modifiers);
            A.CallTo(() => _workflowModifierAttributeRepo.GetByNodeId(10, false)).Returns(modifierAttributes);
            A.CallTo(() => _recipeAttributeRepo.GetByNodeId(10, false)).Returns(recipeAttributes);
            A.CallTo(() => _machineAttributeRepo.GetByNodeId(10, false)).Returns(machineAttributes);
            A.CallTo(() => _edgeRepo.GetByWorkflow(workflowId, false)).Returns(new List<WorkflowEdge>());
            A.CallTo(() => _targetRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowTarget>());
            A.CallTo(() => _productNodeRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowProductNode>());
            A.CallTo(() => _recipeRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowRecipe>());

            // Act
            var result = await _service.GetByWorkflowId(workflowId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Nodes);
            Assert.Equal(10, result.Nodes[0].Node.Node_Id);
            Assert.Single(result.Nodes[0].Modifiers);
            Assert.Equal(20, result.Nodes[0].Modifiers[0].Modifier.Workflow_Node_Modifier_Id);
            Assert.Single(result.Nodes[0].Modifiers[0].ModifierAttributes);
            Assert.Equal(30, result.Nodes[0].Modifiers[0].ModifierAttributes[0].Workflow_Modifier_Attribute_Id);
            Assert.Single(result.Nodes[0].RecipeAttributes);
            Assert.Equal(40, result.Nodes[0].RecipeAttributes[0].Workflow_Recipe_Attribute_Id);
            Assert.Single(result.Nodes[0].MachineAttributes);
            Assert.Equal(50, result.Nodes[0].MachineAttributes[0].Workflow_Machine_Attribute_Id);
        }

        [Fact]
        public async Task WorkflowUpdate_ShouldHandleNewNodesAndAssignIdsToDependencies()
        {
            // Arrange
            int workflowId = 1;
            
            // Original chart is empty
            A.CallTo(() => _nodeRepo.GetByWorkflow(workflowId, false)).Returns(new List<WorkflowNode>());
            A.CallTo(() => _targetRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowTarget>());
            A.CallTo(() => _edgeRepo.GetByWorkflow(workflowId, false)).Returns(new List<WorkflowEdge>());
            A.CallTo(() => _productNodeRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowProductNode>());
            A.CallTo(() => _recipeRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowRecipe>());

            // New chart with a new node (ID = 0)
            var newNode = new WorkflowNode { Node_Id = 0, Workflow_Id = workflowId, Puid = "new-node", Recipe_Id = 1, Recipe_Version = 1 };
            var newModifier = new WorkflowNodeModifier { Workflow_Node_Modifier_Id = 0, Workflow_Node_Id = 0, Modifier_Id = 2, Modifier_Version = 1 };
            var newModifierAttribute = new WorkflowModifierAttribute { Workflow_Modifier_Attribute_Id = 0, Workflow_Node_Id = 0, Workflow_Node_Modifier_Id = 0, Modifier_Id = 2, Attribute_Id = 3, Flat_Bonus = 0, Percent_Bonus = 0, Multiplicative_Bonus = 1 };
            var newRecipeAttribute = new WorkflowRecipeAttribute { Workflow_Recipe_Attribute_Id = 0, Workflow_Node_Id = 0, Attribute_Id = 4, Rate = 1.0 };
            var newMachineAttribute = new WorkflowMachineAttribute { Workflow_Machine_Attribute_Id = 0, Workflow_Node_Id = 0, Attribute_Id = 5, Rate = 1.0 };

            var fullNode = new FullNode
            {
                Node = newNode,
                Modifiers = new List<FullWorkflowModifier>
                {
                    new FullWorkflowModifier
                    {
                        Modifier = newModifier,
                        ModifierAttributes = new List<WorkflowModifierAttribute> { newModifierAttribute }
                    }
                },
                RecipeAttributes = new List<WorkflowRecipeAttribute> { newRecipeAttribute },
                MachineAttributes = new List<WorkflowMachineAttribute> { newMachineAttribute }
            };

            var nodeChart = new NodeChart
            {
                Nodes = new List<FullNode> { fullNode },
                Targets = new List<WorkflowTarget>(),
                Edges = new List<WorkflowEdge>(),
                ProductNodes = new List<WorkflowProductNode>(),
                PreferredRecipes = new List<WorkflowRecipe>()
            };

            // Setup repository behavior for adding
            A.CallTo(() => _nodeRepo.AddWorkflowNodes(A<List<WorkflowNode>>._))
                .Invokes((List<WorkflowNode> nodes) => {
                    foreach(var n in nodes) n.Node_Id = 100; // Simulate DB assigning ID
                });

            A.CallTo(() => _modifierRepo.AddWorkflowNodeModifiers(A<List<WorkflowNodeModifier>>._))
                .Invokes((List<WorkflowNodeModifier> mods) => {
                    foreach(var m in mods) m.Workflow_Node_Modifier_Id = 200; // Simulate DB assigning ID
                });

            // Act
            await _service.WorkflowUpdate(workflowId, nodeChart);

            // Assert
            // 1. Node ID was assigned
            Assert.Equal(100, newNode.Node_Id);

            // 2. Modifiers and recipe/machine attributes assigned the new node_id
            Assert.Equal(100, newModifier.Workflow_Node_Id);
            Assert.Equal(100, newRecipeAttribute.Workflow_Node_Id);
            Assert.Equal(100, newMachineAttribute.Workflow_Node_Id);

            // 3. Modifier ID was assigned
            Assert.Equal(200, newModifier.Workflow_Node_Modifier_Id);

            // 4. Modifier attributes assigned both workflow_modifier_id and node_id
            Assert.Equal(100, newModifierAttribute.Workflow_Node_Id);
            Assert.Equal(200, newModifierAttribute.Workflow_Node_Modifier_Id);
            
            // Verify repository calls
            A.CallTo(() => _nodeRepo.AddWorkflowNodes(A<List<WorkflowNode>>.That.Contains(newNode))).MustHaveHappenedOnceExactly();
            A.CallTo(() => _modifierRepo.AddWorkflowNodeModifiers(A<List<WorkflowNodeModifier>>.That.Contains(newModifier))).MustHaveHappenedOnceExactly();
            A.CallTo(() => _recipeAttributeRepo.AddWorkflowRecipeAttributes(A<List<WorkflowRecipeAttribute>>.That.Contains(newRecipeAttribute))).MustHaveHappenedOnceExactly();
            A.CallTo(() => _machineAttributeRepo.AddWorkflowMachineAttributes(A<List<WorkflowMachineAttribute>>.That.Contains(newMachineAttribute))).MustHaveHappenedOnceExactly();
            A.CallTo(() => _workflowModifierAttributeRepo.AddWorkflowModifierAttributes(A<List<WorkflowModifierAttribute>>.That.Contains(newModifierAttribute))).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WorkflowUpdate_ShouldUpdateExistingNodesAndAttributes()
        {
            // Arrange
            int workflowId = 1;
            var originalNode = new WorkflowNode { Node_Id = 10, Workflow_Id = workflowId, Puid = "node1", Recipe_Id = 1, Recipe_Version = 1 };
            var originalRecipeAttr = new WorkflowRecipeAttribute { Workflow_Recipe_Attribute_Id = 40, Workflow_Node_Id = 10, Attribute_Id = 1, Rate = 1.0 };
            
            A.CallTo(() => _nodeRepo.GetByWorkflow(workflowId, false)).Returns(new List<WorkflowNode> { originalNode });
            A.CallTo(() => _recipeAttributeRepo.GetByNodeId(10, false)).Returns(new List<WorkflowRecipeAttribute> { originalRecipeAttr });
            A.CallTo(() => _modifierRepo.GetByNodeId(10, false)).Returns(new List<WorkflowNodeModifier>());
            A.CallTo(() => _workflowModifierAttributeRepo.GetByNodeId(10, false)).Returns(new List<WorkflowModifierAttribute>());
            A.CallTo(() => _machineAttributeRepo.GetByNodeId(10, false)).Returns(new List<WorkflowMachineAttribute>());
            A.CallTo(() => _targetRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowTarget>());
            A.CallTo(() => _edgeRepo.GetByWorkflow(workflowId, false)).Returns(new List<WorkflowEdge>());
            A.CallTo(() => _productNodeRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowProductNode>());
            A.CallTo(() => _recipeRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowRecipe>());

            // Updated chart: change a value in the node and the attribute
            var updatedNode = new WorkflowNode { Node_Id = 10, Workflow_Id = workflowId, Puid = "node1", Recipe_Id = 1, Recipe_Version = 2 }; // Changed version
            var updatedRecipeAttr = new WorkflowRecipeAttribute { Workflow_Recipe_Attribute_Id = 40, Workflow_Node_Id = 10, Attribute_Id = 1, Rate = 2.0 }; // Changed rate

            var nodeChart = new NodeChart
            {
                Nodes = new List<FullNode> 
                { 
                    new FullNode 
                    { 
                        Node = updatedNode, 
                        RecipeAttributes = new List<WorkflowRecipeAttribute> { updatedRecipeAttr },
                        Modifiers = new List<FullWorkflowModifier>(),
                        MachineAttributes = new List<WorkflowMachineAttribute>()
                    } 
                },
                Targets = new List<WorkflowTarget>(),
                Edges = new List<WorkflowEdge>(),
                ProductNodes = new List<WorkflowProductNode>(),
                PreferredRecipes = new List<WorkflowRecipe>()
            };

            // Act
            await _service.WorkflowUpdate(workflowId, nodeChart);

            // Assert
            A.CallTo(() => _nodeRepo.UpdateWorkflowNodes(A<List<WorkflowNode>>.That.Matches(l => l.Any(n => n.Recipe_Version == 2)))).MustHaveHappenedOnceExactly();
            A.CallTo(() => _recipeAttributeRepo.UpdateWorkflowRecipeAttributes(A<List<WorkflowRecipeAttribute>>.That.Matches(l => l.Any(a => a.Rate == 2.0)))).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WorkflowUpdate_ShouldDeleteRemovedItems()
        {
            // Arrange
            int workflowId = 1;
            var originalNode = new WorkflowNode { Node_Id = 10, Workflow_Id = workflowId, Puid = "node1", Recipe_Id = 1, Recipe_Version = 1 };
            
            A.CallTo(() => _nodeRepo.GetByWorkflow(workflowId, false)).Returns(new List<WorkflowNode> { originalNode });
            A.CallTo(() => _modifierRepo.GetByNodeId(10, false)).Returns(new List<WorkflowNodeModifier>());
            A.CallTo(() => _workflowModifierAttributeRepo.GetByNodeId(10, false)).Returns(new List<WorkflowModifierAttribute>());
            A.CallTo(() => _recipeAttributeRepo.GetByNodeId(10, false)).Returns(new List<WorkflowRecipeAttribute>());
            A.CallTo(() => _machineAttributeRepo.GetByNodeId(10, false)).Returns(new List<WorkflowMachineAttribute>());
            A.CallTo(() => _targetRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowTarget>());
            A.CallTo(() => _edgeRepo.GetByWorkflow(workflowId, false)).Returns(new List<WorkflowEdge>());
            A.CallTo(() => _productNodeRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowProductNode>());
            A.CallTo(() => _recipeRepo.GetByWorkflowId(workflowId, false)).Returns(new List<WorkflowRecipe>());

            // Empty chart (deleting the node)
            var nodeChart = new NodeChart
            {
                Nodes = new List<FullNode>(),
                Targets = new List<WorkflowTarget>(),
                Edges = new List<WorkflowEdge>(),
                ProductNodes = new List<WorkflowProductNode>(),
                PreferredRecipes = new List<WorkflowRecipe>()
            };

            // Act
            await _service.WorkflowUpdate(workflowId, nodeChart);

            // Assert
            A.CallTo(() => _nodeRepo.DeleteWorkflowNodes(A<List<int>>.That.Contains(10))).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WorkflowEdgeUpdate_ShouldUpdateEdges()
        {
            // Arrange
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

            // Act
            await _service.WorkflowEdgeUpdate(workflowId, nodeChart);

            // Assert
            A.CallTo(() => _edgeRepo.DeleteWorkflowEdges(A<List<int>>.That.Contains(500))).MustHaveHappenedOnceExactly();
            A.CallTo(() => _edgeRepo.AddWorkflowEdges(A<List<WorkflowEdge>>.That.Contains(newEdge))).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task NodePuidExists_ShouldReturnRepoResult()
        {
            // Arrange
            string puid = "some-puid";
            A.CallTo(() => _nodeRepo.PuidExists(puid)).Returns(true);

            // Act
            var result = await _service.NodePuidExists(puid);

            // Assert
            Assert.True(result);
            A.CallTo(() => _nodeRepo.PuidExists(puid)).MustHaveHappenedOnceExactly();
        }
    }
}
