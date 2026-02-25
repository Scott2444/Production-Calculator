using FakeItEasy;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ProductionCalculator.Business.Tests.Services
{
    public class WorkflowChartAssemblerTests
    {
        private readonly IMachineCalculator _machineCalculator;
        private readonly WorkflowChartAssembler _assembler;

        public WorkflowChartAssemblerTests()
        {
            _machineCalculator = A.Fake<IMachineCalculator>();
            _assembler = new WorkflowChartAssembler(_machineCalculator);
        }

        private ProjectObjects CreateEmptyProjectObjects()
        {
            return new ProjectObjects
            {
                Products = [],
                Attributes = [],
                Recipes = [],
                RecipeProducts = [],
                RecipeAttributes = [],
                Machines = [],
                MachineRecipes = [],
                MachineAttributes = [],
                Modifiers = [],
                ModifierAttributes = []
            };
        }

        private Workflow CreateTestWorkflow()
        {
            return new Workflow
            {
                Workflow_Id = 1,
                Project_Id = 1,
                Puid = "W1",
                Name = "Test Workflow",
                Created_At = DateTime.UtcNow,
                Last_Updated = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task RebuildChartNodes_ShouldReuseExistingNodes()
        {
            // Arrange
            var workflow = CreateTestWorkflow();
            var recipeId = 10;
            var currentChart = new NodeChart
            {
                Nodes = new List<FullNode>
                {
                    new FullNode
                    {
                        Node = new WorkflowNode { Node_Id = 100, Workflow_Id = 1, Puid = "existing-puid", Recipe_Id = recipeId, Recipe_Version = 1 },
                        Modifiers = [],
                        RecipeAttributes = [],
                        MachineAttributes = []
                    }
                },
                Targets = [],
                Edges = [],
                ProductNodes = [],
                PreferredRecipes = []
            };

            var recipeRates = new Dictionary<int, double> { { recipeId, 5.0 } };
            var projectObjects = CreateEmptyProjectObjects();
            projectObjects.Recipes = [new Recipe { Recipe_Id = recipeId, Project_Id = 1, Puid = "R1", Name = "Recipe 1", Base_Crafting_Time = 1, Version = 2, Created_At = DateTime.UtcNow, Last_Updated = DateTime.UtcNow }];

            // Act
            var result = await _assembler.RebuildChartNodes(currentChart, recipeRates, projectObjects, workflow, _ => Task.FromResult(false));

            // Assert
            Assert.Single(result.Nodes);
            Assert.Equal(100, result.Nodes[0].Node.Node_Id); // Reused
            Assert.Equal(2, result.Nodes[0].Node.Recipe_Version); // Updated version
            Assert.Equal(5.0, result.Nodes[0].Node.Calculated_Target_Rate);
        }

        [Fact]
        public async Task RebuildChartNodes_ShouldCreateNewNodesAndAssignPuid()
        {
            // Arrange
            var workflow = CreateTestWorkflow();
            var recipeId = 20;
            var currentChart = new NodeChart { Nodes = [], Targets = [], Edges = [], ProductNodes = [], PreferredRecipes = [] };
            var recipeRates = new Dictionary<int, double> { { recipeId, 10.0 } };
            
            var projectObjects = CreateEmptyProjectObjects();
            projectObjects.Recipes = [new Recipe { Recipe_Id = recipeId, Project_Id = 1, Puid = "R2", Name = "Recipe 2", Base_Crafting_Time = 1, Version = 1, Created_At = DateTime.UtcNow, Last_Updated = DateTime.UtcNow }];

            // Act
            var result = await _assembler.RebuildChartNodes(currentChart, recipeRates, projectObjects, workflow, _ => Task.FromResult(false));

            // Assert
            Assert.Single(result.Nodes);
            Assert.Equal(0, result.Nodes[0].Node.Node_Id); // New node
            Assert.NotNull(result.Nodes[0].Node.Puid);
            Assert.Equal(recipeId, result.Nodes[0].Node.Recipe_Id);
        }

        [Fact]
        public async Task RebuildChartNodes_ShouldHandleImportRecipesAsProductNodes()
        {
            // Arrange
            var workflow = CreateTestWorkflow();
            var importRecipeId = 30;
            var productId = 100;
            var currentChart = new NodeChart { Nodes = [], Targets = [], Edges = [], ProductNodes = [], PreferredRecipes = [] };
            var recipeRates = new Dictionary<int, double> { { importRecipeId, 15.0 } };
            
            var projectObjects = CreateEmptyProjectObjects();
            projectObjects.Recipes = [new Recipe { Recipe_Id = importRecipeId, Project_Id = 1, Puid = "IMPORT_P1", Name = "Import Product 1", Base_Crafting_Time = 1, Version = 1, Created_At = DateTime.UtcNow, Last_Updated = DateTime.UtcNow }];
            projectObjects.RecipeProducts = [new RecipeProduct { Recipe_Product_Id = 1, Recipe_Id = importRecipeId, Product_Id = productId, Quantity = 1, Is_Input = false }];
            projectObjects.Products = [new Product { Product_Id = productId, Project_Id = 1, Puid = "P1", Name = "Product 1", Created_At = DateTime.UtcNow, Last_Updated = DateTime.UtcNow }];

            // Act
            var result = await _assembler.RebuildChartNodes(currentChart, recipeRates, projectObjects, workflow, _ => Task.FromResult(false));

            // Assert
            Assert.Empty(result.Nodes); // Should not create a workflow node for import recipes
            var productNode = Assert.Single(result.ProductNodes);
            Assert.Equal(productId, productNode.Product_Id);
            Assert.Equal(15.0, productNode.Calculated_Flow_Rate);
            // Note: The logic in RebuildChartNodes sets Is_External based on currentChart. 
            // If it's not in currentChart, it stays false initially and then we might need to check how it works.
        }

        [Fact]
        public void RebuildChartEdges_ShouldAssembleEdgesAndPreserveIds()
        {
            // Arrange
            var projectObjects = CreateEmptyProjectObjects();
            projectObjects.RecipeProducts = new List<RecipeProduct>
            {
                new RecipeProduct { Recipe_Product_Id = 1, Recipe_Id = 10, Product_Id = 100, Quantity = 1, Is_Input = false }
            };

            var currentChart = new NodeChart
            {
                Nodes = [],
                Edges = new List<WorkflowEdge>
                {
                    new WorkflowEdge { Workflow_Edge_Id = 500, Workflow_Id = 1, Producer_Node_Id = 1000, Consumer_Node_Id = null, Product_Node_Id = 2000, Calculated_Flow_Rate = 0, Actual_Flow_Rate = 0 }
                },
                Targets = [],
                ProductNodes = [],
                PreferredRecipes = []
            };

            var updatedChart = new NodeChart
            {
                Nodes = new List<FullNode>
                {
                    new FullNode 
                    { 
                        Node = new WorkflowNode { Node_Id = 1000, Workflow_Id = 1, Puid = "N1", Recipe_Id = 10, Recipe_Version = 1, Calculated_Target_Rate = 10 },
                        Modifiers = [],
                        RecipeAttributes = [],
                        MachineAttributes = []
                    }
                },
                ProductNodes = new List<WorkflowProductNode>
                {
                    new WorkflowProductNode { Workflow_Product_Node_Id = 2000, Workflow_Id = 1, Product_Id = 100, Calculated_Flow_Rate = 10, Actual_Flow_Rate_In = 0, Actual_Flow_Rate_Out = 0, Is_External = false }
                },
                Edges = [],
                Targets = [],
                PreferredRecipes = []
            };

            // Act
            var result = _assembler.RebuildChartEdges(currentChart, updatedChart, projectObjects);

            // Assert
            Assert.Single(result.Edges);
            Assert.Equal(500, result.Edges[0].Workflow_Edge_Id); // Preserved ID
            Assert.Equal(1000, result.Edges[0].Producer_Node_Id);
            Assert.Equal(2000, result.Edges[0].Product_Node_Id);
        }

        [Fact]
        public void UpdateChartRates_ShouldUpdateAllComponents()
        {
            // Arrange
            var recipeRates = new Dictionary<int, double> { { 10, 50.0 } };
            var projectObjects = CreateEmptyProjectObjects();
            projectObjects.RecipeProducts = new List<RecipeProduct>
            {
                new RecipeProduct { Recipe_Product_Id = 1, Recipe_Id = 10, Product_Id = 100, Quantity = 2, Is_Input = false }
            };

            var chart = new NodeChart
            {
                Nodes = new List<FullNode>
                {
                    new FullNode 
                    { 
                        Node = new WorkflowNode { Node_Id = 1, Workflow_Id = 1, Puid = "N1", Recipe_Id = 10, Recipe_Version = 1 },
                        Modifiers = [],
                        RecipeAttributes = [],
                        MachineAttributes = []
                    }
                },
                Edges = new List<WorkflowEdge>
                {
                    new WorkflowEdge { Workflow_Edge_Id = 1, Workflow_Id = 1, Producer_Node_Id = 1, Consumer_Node_Id = null, Product_Node_Id = 10, Calculated_Flow_Rate = 0, Actual_Flow_Rate = 0 }
                },
                ProductNodes = new List<WorkflowProductNode>
                {
                    new WorkflowProductNode { Workflow_Product_Node_Id = 10, Workflow_Id = 1, Product_Id = 100, Calculated_Flow_Rate = 0, Actual_Flow_Rate_In = 0, Actual_Flow_Rate_Out = 0, Is_External = false }
                },
                Targets = [],
                PreferredRecipes = []
            };

            // Act
            var result = _assembler.UpdateChartRates(chart, recipeRates, projectObjects);

            // Assert
            Assert.Equal(50.0, result.Nodes[0].Node.Calculated_Actual_Rate);
            Assert.Equal(100.0, result.Edges[0].Calculated_Flow_Rate); // 2 * 50
            Assert.Equal(100.0, result.ProductNodes[0].Actual_Flow_Rate_Out);
        }

        [Fact]
        public void PruneDeletedComponents_ShouldRemoveMissingItems()
        {
            // Arrange
            var projectObjects = CreateEmptyProjectObjects();
            projectObjects.Products = [new Product { Product_Id = 1, Name = "P1", Project_Id = 1, Puid = "P1", Created_At = DateTime.UtcNow, Last_Updated = DateTime.UtcNow }];
            projectObjects.Recipes = [new Recipe { Recipe_Id = 1, Project_Id = 1, Puid = "R1", Name = "R1", Base_Crafting_Time = 1, Version = 1, Created_At = DateTime.UtcNow, Last_Updated = DateTime.UtcNow }];

            var chart = new NodeChart
            {
                Targets = new List<WorkflowTarget>
                {
                    new WorkflowTarget { Workflow_Target_Id = 1, Workflow_Id = 1, Product_Id = 1, Target_Rate = 10 },
                    new WorkflowTarget { Workflow_Target_Id = 2, Workflow_Id = 1, Product_Id = 999, Target_Rate = 10 } // Missing
                },
                ProductNodes = new List<WorkflowProductNode>
                {
                    new WorkflowProductNode { Workflow_Product_Node_Id = 1, Workflow_Id = 1, Product_Id = 1, Calculated_Flow_Rate = 0, Actual_Flow_Rate_In = 0, Actual_Flow_Rate_Out = 0, Is_External = false },
                    new WorkflowProductNode { Workflow_Product_Node_Id = 2, Workflow_Id = 1, Product_Id = 999, Calculated_Flow_Rate = 0, Actual_Flow_Rate_In = 0, Actual_Flow_Rate_Out = 0, Is_External = false } // Missing
                },
                PreferredRecipes = new List<WorkflowRecipe>
                {
                    new WorkflowRecipe { Workflow_Recipe_Id = 1, Workflow_Id = 1, Recipe_Id = 1 },
                    new WorkflowRecipe { Workflow_Recipe_Id = 2, Workflow_Id = 1, Recipe_Id = 999 } // Missing
                },
                Nodes = [],
                Edges = []
            };

            // Act
            var result = _assembler.PruneDeletedComponents(chart, projectObjects);

            // Assert
            Assert.Single(result.Targets);
            Assert.Equal(1, result.Targets[0].Product_Id);
            Assert.Single(result.ProductNodes);
            Assert.Equal(1, result.ProductNodes[0].Product_Id);
            Assert.Single(result.PreferredRecipes);
            Assert.Equal(1, result.PreferredRecipes[0].Recipe_Id);
        }
    }
}
