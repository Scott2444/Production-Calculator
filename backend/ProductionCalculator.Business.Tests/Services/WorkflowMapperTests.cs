using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Services;

namespace ProductionCalculator.Business.Tests;

public class WorkflowMapperTests
{
    [Fact]
    public void ToResponse_MapsModifierScopedAttributes()
    {
        var mapper = new WorkflowMapper();

        var projectObjects = new ProjectObjects
        {
            Products = [],
            Attributes =
            [
                new ProjectAttribute
                {
                    Attribute_Id = 1,
                    Project_Id = 1,
                    Puid = "attr000001",
                    Name = "Power",
                    Description = "",
                    Unit = "MW",
                    Version = 1,
                    Created_At = DateTime.UtcNow,
                    Last_Updated = DateTime.UtcNow
                }
            ],
            Recipes =
            [
                new Recipe
                {
                    Recipe_Id = 10,
                    Project_Id = 1,
                    Puid = "rec0000001",
                    Name = "Recipe",
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
                    Machine_Id = 20,
                    Project_Id = 1,
                    Puid = "mach000001",
                    Name = "Machine",
                    Description = "",
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
                    Modifier_Id = 30,
                    Project_Id = 1,
                    Puid = "mod0000001",
                    Name = "Modifier",
                    Description = "",
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
                        Node_Id = 1,
                        Workflow_Id = 1,
                        Puid = "node000001",
                        Recipe_Id = 10,
                        Recipe_Version = 1,
                        Machine_Id = 20,
                        Machine_Version = 1,
                        Actual_Machine_Count = 1,
                        Calculated_Machine_Count = 1,
                        Calculated_Target_Rate = 1,
                        Calculated_Actual_Rate = 1
                    },
                    Modifiers =
                    [
                        new FullWorkflowModifier
                        {
                            Modifier = new WorkflowNodeModifier
                            {
                                Workflow_Node_Modifier_Id = 100,
                                Workflow_Node_Id = 1,
                                Modifier_Id = 30,
                                Modifier_Version = 1
                            },
                            ModifierAttributes =
                            [
                                new WorkflowModifierAttribute
                                {
                                    Workflow_Modifier_Attribute_Id = 1,
                                    Workflow_Node_Id = 1,
                                    Workflow_Node_Modifier_Id = 100,
                                    Modifier_Id = 30,
                                    Attribute_Id = 1,
                                    Flat_Bonus = 1,
                                    Percent_Bonus = 2,
                                    Multiplicative_Bonus = 3
                                }
                            ]
                        }
                    ],
                    RecipeAttributes =
                    [
                        new WorkflowRecipeAttribute
                        {
                            Workflow_Recipe_Attribute_Id = 1,
                            Workflow_Node_Id = 1,
                            Attribute_Id = 1,
                            Rate = 9
                        }
                    ],
                    MachineAttributes = []
                }
            ],
            Edges = [],
            Targets = [],
            ProductNodes = [],
            PreferredRecipes = []
        };

        var response = mapper.ToResponse(projectObjects, chart);

        Assert.Single(response.Nodes);
        var node = response.Nodes.Single();
        Assert.Single(node.RecipeAttributes);
        Assert.Equal("attr000001", node.RecipeAttributes[0].Puid);
        Assert.Single(node.Modifiers);
        Assert.Equal("mod0000001", node.Modifiers[0].Puid);
        Assert.Single(node.Modifiers[0].Attributes!);
        Assert.Equal("attr000001", node.Modifiers[0].Attributes![0].AttributePuid);
    }
}
