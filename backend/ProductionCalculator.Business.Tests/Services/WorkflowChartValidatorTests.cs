using System.Diagnostics.CodeAnalysis;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Services;

namespace ProductionCalculator.Business.Tests.Services
{
    [ExcludeFromCodeCoverage]
    public class WorkflowChartValidatorTests
    {
        [Fact]
        public void WorkflowIsUpToDate_ReturnsTrue_ForMatchingVersions()
        {
            var validator = new WorkflowChartValidator();
            var chart = new NodeChart
            {
                Nodes =
                [
                    new FullNode
                    {
                        Node = new WorkflowNode
                        {
                            Node_Id = 1,
                            Workflow_Id = 1,
                            Puid = "node1",
                            Recipe_Id = 10,
                            Recipe_Version = 3,
                            Machine_Id = 20,
                            Machine_Version = 4
                        },
                        Modifiers =
                        [
                            new WorkflowNodeModifier
                            {
                                Workflow_Node_Modifier_Id = 1,
                                Workflow_Node_Id = 1,
                                Modifier_Id = 30,
                                Modifier_Version = 5
                            }
                        ]
                    }
                ],
                Edges = [],
                Targets = [],
                ProductNodes =
                [
                    new WorkflowProductNode
                    {
                        Workflow_Product_Node_Id = 1,
                        Workflow_Id = 1,
                        Product_Id = 40,
                        Calculated_Flow_Rate = 0,
                        Actual_Flow_Rate_In = 0,
                        Actual_Flow_Rate_Out = 0,
                        Is_External = false
                    }
                ],
                PreferredRecipes =
                [
                    new WorkflowRecipe
                    {
                        Workflow_Recipe_Id = 1,
                        Workflow_Id = 1,
                        Recipe_Id = 10
                    }
                ]
            };

            var projectObjects = new ProjectObjects
            {
                Products =
                [
                    new Product
                    {
                        Product_Id = 40,
                        Project_Id = 1,
                        Puid = "p1",
                        Name = "P1",
                        Created_At = DateTime.UtcNow,
                        Last_Updated = DateTime.UtcNow
                    }
                ],
                Attributes = [],
                Recipes =
                [
                    new Recipe
                    {
                        Recipe_Id = 10,
                        Project_Id = 1,
                        Puid = "r1",
                        Name = "R1",
                        Base_Crafting_Time = 1,
                        Version = 3,
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
                        Machine_Id = 20,
                        Project_Id = 1,
                        Puid = "m1",
                        Name = "M1",
                        Base_Speed = 1,
                        Version = 4,
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
                        Modifier_Id = 30,
                        Project_Id = 1,
                        Puid = "mod1",
                        Name = "Mod1",
                        Flat_Bonus = 0,
                        Percent_Bonus = 0,
                        Multiplicative_Bonus = 1,
                        Input_Percent = 0,
                        Output_Percent = 0,
                        Version = 5,
                        Created_At = DateTime.UtcNow,
                        Last_Updated = DateTime.UtcNow
                    }
                ],
                ModifierAttributes = []
            };

            Assert.True(validator.WorkflowIsUpToDate(chart, projectObjects));
        }

        [Fact]
        public void WorkflowIsUpToDate_ReturnsFalse_WhenModifierVersionMismatches()
        {
            var validator = new WorkflowChartValidator();
            var chart = new NodeChart
            {
                Nodes =
                [
                    new FullNode
                    {
                        Node = new WorkflowNode
                        {
                            Node_Id = 1,
                            Workflow_Id = 1,
                            Puid = "node1",
                            Recipe_Id = 10,
                            Recipe_Version = 1,
                            Machine_Id = null,
                            Machine_Version = null
                        },
                        Modifiers =
                        [
                            new WorkflowNodeModifier
                            {
                                Workflow_Node_Modifier_Id = 1,
                                Workflow_Node_Id = 1,
                                Modifier_Id = 30,
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

            var projectObjects = new ProjectObjects
            {
                Products = [],
                Attributes = [],
                Recipes =
                [
                    new Recipe
                    {
                        Recipe_Id = 10,
                        Project_Id = 1,
                        Puid = "r1",
                        Name = "R1",
                        Base_Crafting_Time = 1,
                        Version = 1,
                        Created_At = DateTime.UtcNow,
                        Last_Updated = DateTime.UtcNow
                    }
                ],
                RecipeProducts = [],
                RecipeAttributes = [],
                Machines = [],
                MachineRecipes = [],
                MachineAttributes = [],
                Modifiers =
                [
                    new Modifier
                    {
                        Modifier_Id = 30,
                        Project_Id = 1,
                        Puid = "mod1",
                        Name = "Mod1",
                        Flat_Bonus = 0,
                        Percent_Bonus = 0,
                        Multiplicative_Bonus = 1,
                        Input_Percent = 0,
                        Output_Percent = 0,
                        Version = 2,
                        Created_At = DateTime.UtcNow,
                        Last_Updated = DateTime.UtcNow
                    }
                ],
                ModifierAttributes = []
            };

            Assert.False(validator.WorkflowIsUpToDate(chart, projectObjects));
        }
    }
}
