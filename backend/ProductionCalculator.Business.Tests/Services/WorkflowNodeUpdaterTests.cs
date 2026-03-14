using System.Diagnostics.CodeAnalysis;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Services;
using ProductionCalculator.Business.Records;
using Xunit;

namespace ProductionCalculator.Business.Tests.Services
{
    [ExcludeFromCodeCoverage]
    public class WorkflowNodeUpdaterTests
    {
        private readonly WorkflowNodeUpdater _updater;

        public WorkflowNodeUpdaterTests()
        {
            _updater = new WorkflowNodeUpdater();
        }

        private static Machine CreateMachine(int id, string puid, int version = 1) => new()
        {
            Machine_Id = id,
            Puid = puid,
            Version = version,
            Project_Id = 1,
            Name = $"Machine {id}",
            Base_Speed = 1.0,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };

        private static Modifier CreateModifier(int id, string puid, double inputPercent = 0, double outputPercent = 0) => new()
        {
            Modifier_Id = id,
            Puid = puid,
            Input_Percent = inputPercent,
            Output_Percent = outputPercent,
            Project_Id = 1,
            Name = $"Modifier {id}",
            Flat_Bonus = 0,
            Percent_Bonus = 0,
            Multiplicative_Bonus = 1,
            Version = 1,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };

        private static ProjectAttribute CreateAttribute(int id, string puid) => new()
        {
            Attribute_Id = id,
            Puid = puid,
            Project_Id = 1,
            Name = $"Attribute {id}",
            Version = 1,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };

        private static FullNode CreateFullNode(int machineId, int machineVersion) => new()
        {
            Node = new WorkflowNode
            {
                Node_Id = 1,
                Workflow_Id = 1,
                Puid = "n1",
                Recipe_Id = 1,
                Recipe_Version = 1,
                Machine_Id = machineId,
                Machine_Version = machineVersion,
                Actual_Machine_Count = 1
            },
            Modifiers = [],
            RecipeAttributes = [],
            MachineAttributes = []
        };

        [Fact]
        public void ApplyPutUpdate_MachineChange_RequiresSupplyRecalc()
        {
            // Arrange
            var m1 = CreateMachine(1, "m1");
            var m2 = CreateMachine(2, "m2");
            var projectObjects = new ProjectObjects
            {
                Machines = [m1, m2],
                Products = [], Attributes = [], Recipes = [], RecipeProducts = [], RecipeAttributes = [], MachineRecipes = [], MachineAttributes = [], Modifiers = [], ModifierAttributes = []
            };

            var fullNode = CreateFullNode(m1.Machine_Id, m1.Version);
            var request = new WorkflowNodeRequest
            {
                MachinePuid = m2.Puid,
                ActualMachineCount = 1,
                Modifiers = [],
                RecipeAttributes = [],
                MachineAttributes = []
            };

            // Act
            var impact = _updater.ApplyPutUpdate(fullNode, request, projectObjects);

            // Assert
            Assert.True(impact.RequiresSupplyRecalculation);
            Assert.False(impact.RequiresDemandRecalculation);
            Assert.Equal(m2.Machine_Id, fullNode.Node.Machine_Id);
            Assert.Equal(m2.Version, fullNode.Node.Machine_Version);
        }

        [Fact]
        public void ApplyPutUpdate_MachineCountChange_RequiresSupplyRecalc()
        {
            // Arrange
            var m1 = CreateMachine(1, "m1");
            var projectObjects = new ProjectObjects
            {
                Machines = [m1],
                Products = [], Attributes = [], Recipes = [], RecipeProducts = [], RecipeAttributes = [], MachineRecipes = [], MachineAttributes = [], Modifiers = [], ModifierAttributes = []
            };

            var fullNode = CreateFullNode(m1.Machine_Id, m1.Version);
            fullNode.Node.Actual_Machine_Count = 1;

            var request = new WorkflowNodeRequest
            {
                MachinePuid = m1.Puid,
                ActualMachineCount = 2,
                Modifiers = [],
                RecipeAttributes = [],
                MachineAttributes = []
            };

            // Act
            var impact = _updater.ApplyPutUpdate(fullNode, request, projectObjects);

            // Assert
            Assert.True(impact.RequiresSupplyRecalculation);
            Assert.False(impact.RequiresDemandRecalculation);
            Assert.Equal(2, fullNode.Node.Actual_Machine_Count);
        }

        [Fact]
        public void ApplyPutUpdate_ModifierYieldChange_RequiresDemandRecalc()
        {
            // Arrange
            var m1 = CreateMachine(1, "m1");
            var mod1 = CreateModifier(1, "mod1", inputPercent: 1.1); // Yield is multiplier usually, but the code sums them
            var projectObjects = new ProjectObjects
            {
                Machines = [m1],
                Modifiers = [mod1],
                Products = [], Attributes = [], Recipes = [], RecipeProducts = [], RecipeAttributes = [], MachineRecipes = [], MachineAttributes = [], ModifierAttributes = []
            };

            var fullNode = CreateFullNode(m1.Machine_Id, m1.Version);
            var request = new WorkflowNodeRequest
            {
                MachinePuid = m1.Puid,
                ActualMachineCount = 1,
                Modifiers = [new WorkflowModifierExchange { Puid = "mod1", Attributes = [] }],
                RecipeAttributes = [],
                MachineAttributes = []
            };

            // Act
            var impact = _updater.ApplyPutUpdate(fullNode, request, projectObjects);

            // Assert
            Assert.True(impact.RequiresDemandRecalculation);
            Assert.False(impact.RequiresSupplyRecalculation);
            Assert.Single(fullNode.Modifiers);
            Assert.Equal(mod1.Modifier_Id, fullNode.Modifiers[0].Modifier.Modifier_Id);
        }

        [Fact]
        public void ApplyPutUpdate_ModifierAddedNoYieldChange_RequiresSupplyRecalc()
        {
            // Arrange
            var m1 = CreateMachine(1, "m1");
            var mod1 = CreateModifier(1, "mod1", inputPercent: 0.0, outputPercent: 0.0);
            var projectObjects = new ProjectObjects
            {
                Machines = [m1],
                Modifiers = [mod1],
                Products = [], Attributes = [], Recipes = [], RecipeProducts = [], RecipeAttributes = [], MachineRecipes = [], MachineAttributes = [], ModifierAttributes = []
            };

            var fullNode = CreateFullNode(m1.Machine_Id, m1.Version);
            var request = new WorkflowNodeRequest
            {
                MachinePuid = m1.Puid,
                ActualMachineCount = 1,
                Modifiers = [new WorkflowModifierExchange { Puid = "mod1", Attributes = [] }],
                RecipeAttributes = [],
                MachineAttributes = []
            };

            // Act
            var impact = _updater.ApplyPutUpdate(fullNode, request, projectObjects);

            // Assert
            Assert.False(impact.RequiresDemandRecalculation);
            Assert.True(impact.RequiresSupplyRecalculation);
            Assert.Single(fullNode.Modifiers);
        }

        [Fact]
        public void ApplyPutUpdate_YieldNeutralModifierSwap_RequiresSupplyRecalc()
        {
            // Arrange
            var m1 = CreateMachine(1, "m1");
            var mod1 = CreateModifier(1, "mod1", inputPercent: 0.5);
            var mod2 = CreateModifier(2, "mod2", inputPercent: 0.5);
            var projectObjects = new ProjectObjects
            {
                Machines = [m1],
                Modifiers = [mod1, mod2],
                Products = [], Attributes = [], Recipes = [], RecipeProducts = [], RecipeAttributes = [], MachineRecipes = [], MachineAttributes = [], ModifierAttributes = []
            };

            var fullNode = CreateFullNode(m1.Machine_Id, m1.Version);
            fullNode.Modifiers = [new FullWorkflowModifier { 
                Modifier = new WorkflowNodeModifier { 
                    Workflow_Node_Modifier_Id = 1,
                    Modifier_Id = mod1.Modifier_Id, 
                    Workflow_Node_Id = 1,
                    Modifier_Version = 1
                },
                ModifierAttributes = []
            }];

            var request = new WorkflowNodeRequest
            {
                MachinePuid = m1.Puid,
                ActualMachineCount = 1,
                Modifiers = [new WorkflowModifierExchange { Puid = "mod2", Attributes = [] }],
                RecipeAttributes = [],
                MachineAttributes = []
            };

            // Act
            var impact = _updater.ApplyPutUpdate(fullNode, request, projectObjects);

            // Assert
            Assert.False(impact.RequiresDemandRecalculation); // Yield sum is same (0.5)
            Assert.True(impact.RequiresSupplyRecalculation); // Modifier list changed
            Assert.Single(fullNode.Modifiers);
            Assert.Equal(mod2.Modifier_Id, fullNode.Modifiers[0].Modifier.Modifier_Id);
        }

        [Fact]
        public void ApplyPutUpdate_AttributeChange_NoRecalc()
        {
            // Arrange
            var m1 = CreateMachine(1, "m1");
            var attr1 = CreateAttribute(1, "a1");
            var projectObjects = new ProjectObjects
            {
                Machines = [m1],
                Attributes = [attr1],
                Products = [], Recipes = [], RecipeProducts = [], RecipeAttributes = [], MachineRecipes = [], MachineAttributes = [], Modifiers = [], ModifierAttributes = []
            };

            var fullNode = CreateFullNode(m1.Machine_Id, m1.Version);
            var request = new WorkflowNodeRequest
            {
                MachinePuid = m1.Puid,
                ActualMachineCount = 1,
                Modifiers = [],
                RecipeAttributes = [new AttributeRateRequest { Puid = "a1", Rate = 100 }],
                MachineAttributes = []
            };

            // Act
            var impact = _updater.ApplyPutUpdate(fullNode, request, projectObjects);

            // Assert
            Assert.False(impact.RequiresDemandRecalculation);
            Assert.False(impact.RequiresSupplyRecalculation);
            Assert.Single(fullNode.RecipeAttributes);
            Assert.Equal(attr1.Attribute_Id, fullNode.RecipeAttributes[0].Attribute_Id);
            Assert.Equal(100, fullNode.RecipeAttributes[0].Rate);
        }

        [Fact]
        public void ApplyPutUpdate_NoChange_NoRecalc()
        {
            // Arrange
            var m1 = CreateMachine(1, "m1");
            var projectObjects = new ProjectObjects
            {
                Machines = [m1],
                Products = [], Attributes = [], Recipes = [], RecipeProducts = [], RecipeAttributes = [], MachineRecipes = [], MachineAttributes = [], Modifiers = [], ModifierAttributes = []
            };

            var fullNode = CreateFullNode(m1.Machine_Id, m1.Version);
            var request = new WorkflowNodeRequest
            {
                MachinePuid = m1.Puid,
                ActualMachineCount = 1,
                Modifiers = [],
                RecipeAttributes = [],
                MachineAttributes = []
            };

            // Act
            var impact = _updater.ApplyPutUpdate(fullNode, request, projectObjects);

            // Assert
            Assert.False(impact.RequiresDemandRecalculation);
            Assert.False(impact.RequiresSupplyRecalculation);
        }

        [Fact]
        public void ApplyPutUpdate_MultipleModifiers_CorrectYieldCalculation()
        {
            // Arrange
            var m1 = CreateMachine(1, "m1");
            var mod1 = CreateModifier(1, "mod1", inputPercent: 0.1, outputPercent: 0.2);
            var mod2 = CreateModifier(2, "mod2", inputPercent: 0.3, outputPercent: 0.4);
            var projectObjects = new ProjectObjects
            {
                Machines = [m1],
                Modifiers = [mod1, mod2],
                Products = [], Attributes = [], Recipes = [], RecipeProducts = [], RecipeAttributes = [], MachineRecipes = [], MachineAttributes = [], ModifierAttributes = []
            };

            var fullNode = CreateFullNode(m1.Machine_Id, m1.Version);
            // Case 1: Add mod1 and mod2
            var request = new WorkflowNodeRequest
            {
                MachinePuid = m1.Puid,
                ActualMachineCount = 1,
                Modifiers = [
                    new WorkflowModifierExchange { Puid = "mod1", Attributes = [] },
                    new WorkflowModifierExchange { Puid = "mod2", Attributes = [] }
                ],
                RecipeAttributes = [],
                MachineAttributes = []
            };

            // Act
            var impact = _updater.ApplyPutUpdate(fullNode, request, projectObjects);

            // Assert
            Assert.True(impact.RequiresDemandRecalculation);
            Assert.Equal(2, fullNode.Modifiers.Count);
        }

        [Fact]
        public void ApplyPutUpdate_SmallYieldChangeWithinTolerance_NoDemandRecalc()
        {
            // Arrange
            var m1 = CreateMachine(1, "m1");
            var mod1 = CreateModifier(1, "mod1", inputPercent: 1.0);
            var mod2 = CreateModifier(2, "mod2", inputPercent: 1.0 + 1e-10); // Tolerance is 1e-9
            var projectObjects = new ProjectObjects
            {
                Machines = [m1],
                Modifiers = [mod1, mod2],
                Products = [], Attributes = [], Recipes = [], RecipeProducts = [], RecipeAttributes = [], MachineRecipes = [], MachineAttributes = [], ModifierAttributes = []
            };

            var fullNode = CreateFullNode(m1.Machine_Id, m1.Version);
            fullNode.Modifiers = [new FullWorkflowModifier { 
                Modifier = new WorkflowNodeModifier { Workflow_Node_Modifier_Id = 1, Modifier_Id = mod1.Modifier_Id, Workflow_Node_Id = 1, Modifier_Version = 1 },
                ModifierAttributes = []
            }];

            var request = new WorkflowNodeRequest
            {
                MachinePuid = m1.Puid,
                ActualMachineCount = 1,
                Modifiers = [new WorkflowModifierExchange { Puid = "mod2", Attributes = [] }],
                RecipeAttributes = [],
                MachineAttributes = []
            };

            // Act
            var impact = _updater.ApplyPutUpdate(fullNode, request, projectObjects);

            // Assert
            Assert.False(impact.RequiresDemandRecalculation);
            Assert.True(impact.RequiresSupplyRecalculation); // Still triggers supply because modifier list changed
        }
    }
}
