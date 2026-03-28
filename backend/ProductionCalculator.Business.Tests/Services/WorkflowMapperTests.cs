using System.Diagnostics.CodeAnalysis;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Services;

namespace ProductionCalculator.Business.Tests.Services
{
    [ExcludeFromCodeCoverage]
    public class WorkflowMapperTests
    {
        [Fact]
        public void ToResponse_MapsModifierPuidsAndNodeFields()
        {
            var mapper = new WorkflowMapper();
            var projectObjects = new ProjectObjects
            {
                Products =
                [
                    new Product
                    {
                        Product_Id = 1,
                        Project_Id = 1,
                        Puid = "prod1",
                        Name = "Product 1",
                        Created_At = DateTime.UtcNow,
                        Last_Updated = DateTime.UtcNow
                    }
                ],
                Attributes = [],
                Recipes =
                [
                    new Recipe
                    {
                        Recipe_Id = 100,
                        Project_Id = 1,
                        Puid = "recipe1",
                        Name = "Recipe 1",
                        Base_Crafting_Time = 1,
                        Version = 1,
                        Created_At = DateTime.UtcNow,
                        Last_Updated = DateTime.UtcNow
                    }
                ],
                RecipeProducts = [],
                RecipeAttributes = [],
                Machines =
                [
                    new Machine
                    {
                        Machine_Id = 200,
                        Project_Id = 1,
                        Puid = "machine1",
                        Name = "Machine 1",
                        Base_Speed = 1,
                        Version = 1,
                        Created_At = DateTime.UtcNow,
                        Last_Updated = DateTime.UtcNow
                    }
                ],
                MachineRecipes = [],
                MachineAttributes = [],
                Modifiers =
                [
                    new Modifier
                    {
                        Modifier_Id = 300,
                        Project_Id = 1,
                        Puid = "mod1",
                        Name = "Mod 1",
                        Flat_Bonus = 0,
                        Percent_Bonus = 0,
                        Multiplicative_Bonus = 1,
                        Input_Percent = 0,
                        Output_Percent = 0,
                        Version = 1,
                        Created_At = DateTime.UtcNow,
                        Last_Updated = DateTime.UtcNow
                    }
                ],
                ModifierAttributes = []
            };

            var chart = new NodeChart
            {
                Nodes =
                [
                    new FullNode
                    {
                        Node = new WorkflowNode
                        {
                            Node_Id = 10,
                            Workflow_Id = 1,
                            Puid = "node1",
                            Recipe_Id = 100,
                            Recipe_Version = 1,
                            Machine_Id = 200,
                            Machine_Version = 1,
                            Actual_Machine_Count = 1,
                            Calculated_Machine_Count = 2,
                            Calculated_Target_Rate = 3,
                            Calculated_Actual_Rate = 4
                        },
                        Modifiers =
                        [
                            new WorkflowNodeModifier
                            {
                                Workflow_Node_Modifier_Id = 1,
                                Workflow_Node_Id = 10,
                                Modifier_Id = 300,
                                Modifier_Version = 1
                            }
                        ]
                    }
                ],
                Edges = [],
                Targets = [],
                ProductNodes = [],
                PreferredRecipes = []
            };

            var response = mapper.ToResponse(projectObjects, chart);

            Assert.Single(response.Nodes);
            var node = response.Nodes[0];
            Assert.Equal("node1", node.Puid);
            Assert.Equal("recipe1", node.RecipePuid);
            Assert.Equal("machine1", node.MachinePuid);
            Assert.Single(node.ModifierPuids);
            Assert.Equal("mod1", node.ModifierPuids[0]);
        }
    }
}
