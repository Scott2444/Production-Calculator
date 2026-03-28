using System.Diagnostics.CodeAnalysis;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Services;

namespace ProductionCalculator.Business.Tests.Services
{
    [ExcludeFromCodeCoverage]
    public class WorkflowNodeUpdaterTests
    {
        private static ProjectObjects BuildProjectObjects()
        {
            return new ProjectObjects
            {
                Products = [],
                Attributes = [],
                Recipes =
                [
                    new Recipe
                    {
                        Recipe_Id = 1,
                        Project_Id = 1,
                        Puid = "r1",
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
                        Machine_Id = 1,
                        Project_Id = 1,
                        Puid = "m1",
                        Name = "Machine 1",
                        Base_Speed = 1,
                        Version = 1,
                        Created_At = DateTime.UtcNow,
                        Last_Updated = DateTime.UtcNow
                    },
                    new Machine
                    {
                        Machine_Id = 2,
                        Project_Id = 1,
                        Puid = "m2",
                        Name = "Machine 2",
                        Base_Speed = 1,
                        Version = 2,
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
                        Modifier_Id = 10,
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
                    },
                    new Modifier
                    {
                        Modifier_Id = 11,
                        Project_Id = 1,
                        Puid = "mod2",
                        Name = "Mod 2",
                        Flat_Bonus = 0,
                        Percent_Bonus = 0,
                        Multiplicative_Bonus = 1,
                        Input_Percent = 0.1,
                        Output_Percent = 0,
                        Version = 1,
                        Created_At = DateTime.UtcNow,
                        Last_Updated = DateTime.UtcNow
                    }
                ],
                ModifierAttributes = []
            };
        }

        private static FullNode BuildNode()
        {
            return new FullNode
            {
                Node = new WorkflowNode
                {
                    Node_Id = 100,
                    Workflow_Id = 1,
                    Puid = "node1",
                    Recipe_Id = 1,
                    Recipe_Version = 1,
                    Machine_Id = 1,
                    Machine_Version = 1,
                    Actual_Machine_Count = 1,
                    Calculated_Machine_Count = 1,
                    Calculated_Target_Rate = 1,
                    Calculated_Actual_Rate = 1
                },
                Modifiers = []
            };
        }

        [Fact]
        public void ApplyPutUpdate_UpdatesMachineAndModifiers()
        {
            var updater = new WorkflowNodeUpdater();
            var fullNode = BuildNode();
            var projectObjects = BuildProjectObjects();
            var request = new WorkflowNodeRequest
            {
                MachinePuid = "m2",
                ActualMachineCount = 3,
                ModifierPuids = ["mod1"]
            };

            var impact = updater.ApplyPutUpdate(fullNode, request, projectObjects);

            Assert.False(impact.RequiresDemandRecalculation);
            Assert.True(impact.RequiresSupplyRecalculation);
            Assert.Equal(2, fullNode.Node.Machine_Id);
            Assert.Equal(2, fullNode.Node.Machine_Version);
            Assert.Equal(3, fullNode.Node.Actual_Machine_Count);
            Assert.Single(fullNode.Modifiers);
            Assert.Equal(10, fullNode.Modifiers[0].Modifier_Id);
            Assert.Equal(100, fullNode.Modifiers[0].Workflow_Node_Id);
        }

        [Fact]
        public void ApplyPutUpdate_YieldChangeRequiresDemandRecalc()
        {
            var updater = new WorkflowNodeUpdater();
            var fullNode = BuildNode();
            var projectObjects = BuildProjectObjects();
            var request = new WorkflowNodeRequest
            {
                MachinePuid = "m1",
                ActualMachineCount = 1,
                ModifierPuids = ["mod2"]
            };

            var impact = updater.ApplyPutUpdate(fullNode, request, projectObjects);

            Assert.True(impact.RequiresDemandRecalculation);
            Assert.False(impact.RequiresSupplyRecalculation);
        }

        [Fact]
        public void ApplyPutUpdate_NoChangeRequiresNoRecalc()
        {
            var updater = new WorkflowNodeUpdater();
            var fullNode = BuildNode();
            var projectObjects = BuildProjectObjects();
            var request = new WorkflowNodeRequest
            {
                MachinePuid = "m1",
                ActualMachineCount = 1,
                ModifierPuids = []
            };

            var impact = updater.ApplyPutUpdate(fullNode, request, projectObjects);

            Assert.False(impact.RequiresDemandRecalculation);
            Assert.False(impact.RequiresSupplyRecalculation);
        }
    }
}
