using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Services;

namespace ProductionCalculator.Business.Tests.Services
{
    public class WorkflowChartValidatorTests
    {
        private readonly WorkflowChartValidator _validator;
        private readonly DateTime _now = DateTime.UtcNow;

        public WorkflowChartValidatorTests()
        {
            _validator = new WorkflowChartValidator();
        }

        private Recipe CreateRecipe(int id, int version = 1) => new()
        {
            Recipe_Id = id,
            Project_Id = 1,
            Puid = $"r{id}",
            Name = $"Recipe {id}",
            Base_Crafting_Time = 1,
            Version = version,
            Created_At = _now,
            Last_Updated = _now
        };

        private Machine CreateMachine(int id, int version = 1) => new()
        {
            Machine_Id = id,
            Project_Id = 1,
            Puid = $"m{id}",
            Name = $"Machine {id}",
            Base_Speed = 1,
            Version = version,
            Created_At = _now,
            Last_Updated = _now
        };

        private Modifier CreateModifier(int id, int version = 1) => new()
        {
            Modifier_Id = id,
            Project_Id = 1,
            Puid = $"mod{id}",
            Name = $"Modifier {id}",
            Flat_Bonus = 0,
            Percent_Bonus = 0,
            Multiplicative_Bonus = 1,
            Input_Percent = 0,
            Output_Percent = 0,
            Version = version,
            Created_At = _now,
            Last_Updated = _now
        };

        private Product CreateProduct(int id) => new()
        {
            Product_Id = id,
            Project_Id = 1,
            Puid = $"p{id}",
            Name = $"Product {id}",
            Created_At = _now,
            Last_Updated = _now
        };

        private ProjectAttribute CreateAttribute(int id, int version = 1) => new()
        {
            Attribute_Id = id,
            Project_Id = 1,
            Puid = $"a{id}",
            Name = $"Attribute {id}",
            Version = version,
            Created_At = _now,
            Last_Updated = _now
        };

        private FullNode CreateFullNode(int nodeId, int recipeId, int recipeVersion, int? machineId = null, int? machineVersion = null) => new()
        {
            Node = new WorkflowNode
            {
                Node_Id = nodeId,
                Workflow_Id = 1,
                Puid = $"n{nodeId}",
                Recipe_Id = recipeId,
                Recipe_Version = recipeVersion,
                Machine_Id = machineId,
                Machine_Version = machineVersion
            },
            Modifiers = [],
            RecipeAttributes = [],
            MachineAttributes = []
        };

        private ProjectObjects CreateProjectObjects() => new()
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

        [Fact]
        public void WorkflowIsUpToDate_AllUpToDate_ReturnsTrue()
        {
            var recipe = CreateRecipe(1);
            var machine = CreateMachine(1);
            var modifier = CreateModifier(1);
            var product = CreateProduct(1);
            var attribute = CreateAttribute(1);

            var projectObjects = CreateProjectObjects();
            projectObjects.Recipes = [recipe];
            projectObjects.Machines = [machine];
            projectObjects.Modifiers = [modifier];
            projectObjects.Products = [product];
            projectObjects.Attributes = [attribute];

            var fullNode = CreateFullNode(1, 1, 1, 1, 1);
            fullNode.Modifiers = [new FullWorkflowModifier {
                Modifier = new WorkflowNodeModifier { Workflow_Node_Modifier_Id = 1, Workflow_Node_Id = 1, Modifier_Id = 1, Modifier_Version = 1 },
                ModifierAttributes = [new WorkflowModifierAttribute { Workflow_Modifier_Attribute_Id = 1, Workflow_Node_Id = 1, Workflow_Node_Modifier_Id = 1, Attribute_Id = 1, Flat_Bonus = 0, Percent_Bonus = 0, Multiplicative_Bonus = 1 }]
            }];
            fullNode.RecipeAttributes = [new WorkflowRecipeAttribute { Workflow_Recipe_Attribute_Id = 1, Workflow_Node_Id = 1, Attribute_Id = 1, Rate = 1 }];
            fullNode.MachineAttributes = [new WorkflowMachineAttribute { Workflow_Machine_Attribute_Id = 1, Workflow_Node_Id = 1, Attribute_Id = 1, Rate = 1 }];

            var nodeChart = new NodeChart
            {
                Nodes = [fullNode],
                ProductNodes = [new WorkflowProductNode { Workflow_Product_Node_Id = 1, Workflow_Id = 1, Product_Id = 1, Calculated_Flow_Rate = 0, Actual_Flow_Rate_In = 0, Actual_Flow_Rate_Out = 0, Is_External = false }],
                PreferredRecipes = [new WorkflowRecipe { Workflow_Recipe_Id = 1, Workflow_Id = 1, Recipe_Id = 1 }],
                Edges = [],
                Targets = []
            };

            var result = _validator.WorkflowIsUpToDate(nodeChart, projectObjects);

            Assert.True(result);
        }

        public enum InvalidType { Missing, OutOfDate }
        public enum EntityType { Recipe, Machine, Modifier, Product, PreferredRecipe, Attribute }

        [Theory]
        [InlineData(EntityType.Recipe, InvalidType.Missing)]
        [InlineData(EntityType.Recipe, InvalidType.OutOfDate)]
        [InlineData(EntityType.Machine, InvalidType.Missing)]
        [InlineData(EntityType.Machine, InvalidType.OutOfDate)]
        [InlineData(EntityType.Modifier, InvalidType.Missing)]
        [InlineData(EntityType.Modifier, InvalidType.OutOfDate)]
        [InlineData(EntityType.Product, InvalidType.Missing)]
        [InlineData(EntityType.PreferredRecipe, InvalidType.Missing)]
        [InlineData(EntityType.Attribute, InvalidType.Missing)]
        public void WorkflowIsUpToDate_InvalidProjectData_ReturnsFalse(EntityType entityType, InvalidType invalidType)
        {
            // Arrange
            var projectObjects = CreateProjectObjects();
            
            // Standard "Up to Date" objects
            var recipe = CreateRecipe(1, version: 1);
            var machine = CreateMachine(1, version: 1);
            var modifier = CreateModifier(1, version: 1);
            var product = CreateProduct(1);
            var attribute = CreateAttribute(1, version: 1);

            // Chart that is up to date with the above
            var fullNode = CreateFullNode(1, 1, 1, 1, 1);
            fullNode.Modifiers = [new FullWorkflowModifier {
                Modifier = new WorkflowNodeModifier { Workflow_Node_Modifier_Id = 1, Workflow_Node_Id = 1, Modifier_Id = 1, Modifier_Version = 1 },
                ModifierAttributes = []
            }];
            fullNode.RecipeAttributes = [new WorkflowRecipeAttribute { Workflow_Recipe_Attribute_Id = 1, Workflow_Node_Id = 1, Attribute_Id = 1, Rate = 1 }];

            var nodeChart = new NodeChart
            {
                Nodes = [fullNode],
                ProductNodes = [new WorkflowProductNode { Workflow_Product_Node_Id = 1, Workflow_Id = 1, Product_Id = 1, Calculated_Flow_Rate = 0, Actual_Flow_Rate_In = 0, Actual_Flow_Rate_Out = 0, Is_External = false }],
                PreferredRecipes = [new WorkflowRecipe { Workflow_Recipe_Id = 1, Workflow_Id = 1, Recipe_Id = 1 }],
                Edges = [], Targets = []
            };

            // Apply "Invalidity"
            switch (entityType)
            {
                case EntityType.Recipe:
                    if (invalidType == InvalidType.OutOfDate) projectObjects.Recipes = [CreateRecipe(1, version: 2)];
                    break;
                case EntityType.Machine:
                    projectObjects.Recipes = [recipe];
                    if (invalidType == InvalidType.OutOfDate) projectObjects.Machines = [CreateMachine(1, version: 2)];
                    break;
                case EntityType.Modifier:
                    projectObjects.Recipes = [recipe];
                    projectObjects.Machines = [machine];
                    if (invalidType == InvalidType.OutOfDate) projectObjects.Modifiers = [CreateModifier(1, version: 2)];
                    break;
                case EntityType.Product:
                    projectObjects.Recipes = [recipe];
                    projectObjects.Machines = [machine];
                    projectObjects.Modifiers = [modifier];
                    // missing product 1
                    break;
                case EntityType.PreferredRecipe:
                    projectObjects.Recipes = [recipe];
                    projectObjects.Machines = [machine];
                    projectObjects.Modifiers = [modifier];
                    projectObjects.Products = [product];
                    // Node uses recipe 1, but preferred recipe uses recipe 2 (which is missing)
                    nodeChart.PreferredRecipes = [new WorkflowRecipe { Workflow_Recipe_Id = 1, Workflow_Id = 1, Recipe_Id = 2 }];
                    break;
                case EntityType.Attribute:
                    projectObjects.Recipes = [recipe];
                    projectObjects.Machines = [machine];
                    projectObjects.Modifiers = [modifier];
                    projectObjects.Products = [product];
                    // Node uses attribute 1 (which is missing)
                    break;
            }

            // Fill in the rest of project objects to avoid failures elsewhere
            if (projectObjects.Recipes.Count == 0 && entityType != EntityType.Recipe && entityType != EntityType.PreferredRecipe) projectObjects.Recipes = [recipe];
            if (projectObjects.Machines.Count == 0 && entityType != EntityType.Machine) projectObjects.Machines = [machine];
            if (projectObjects.Modifiers.Count == 0 && entityType != EntityType.Modifier) projectObjects.Modifiers = [modifier];
            if (projectObjects.Products.Count == 0 && entityType != EntityType.Product) projectObjects.Products = [product];
            if (projectObjects.Attributes.Count == 0 && entityType != EntityType.Attribute) projectObjects.Attributes = [attribute];

            // Act
            var result = _validator.WorkflowIsUpToDate(nodeChart, projectObjects);

            // Assert
            Assert.False(result);
        }
    }
}

