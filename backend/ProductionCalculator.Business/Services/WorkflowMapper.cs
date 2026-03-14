using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.APIModels;

namespace ProductionCalculator.Business.Services
{
    public class WorkflowMapper : IWorkflowMapper
    {
        public WorkflowMapper()
        {
        }

        /// <summary>
        /// Maps the internal node chart representation to a response object for the API
        /// </summary>
        /// <param name="projectObjects">The project objects containing the data to map from</param>
        /// <param name="chart">The node chart to map</param>
        /// <returns>A WorkflowChartResponse object containing the mapped data</returns>
        /// <exception cref="ArgumentException">Thrown when a referenced object in the chart is not found in the project objects</exception>
        public WorkflowChartResponse ToResponse(ProjectObjects projectObjects, NodeChart chart)
        {
            var response = new WorkflowChartResponse
            {
                Nodes = new List<WorkflowNodeResponse>(),
                Edges = new List<WorkflowEdgeResponse>(),
                Targets = new List<WorkflowTargetExchange>(),
                ProductNodes = new List<WorkflowProductNodeResponse>(),
                PreferredRecipes = new List<string>()
            };
            foreach (var fullNode in chart.Nodes)
            {
                var nodeResponse = new WorkflowNodeResponse
                {
                    Puid = fullNode.Node.Puid,
                    RecipePuid = projectObjects.Recipes.FirstOrDefault(r => r.Recipe_Id == fullNode.Node.Recipe_Id)?.Puid ?? throw new ArgumentException($"Recipe with id {fullNode.Node.Recipe_Id} not found in project objects"),
                    MachinePuid = fullNode.Node.Machine_Id.HasValue ? projectObjects.Machines.FirstOrDefault(m => m.Machine_Id == fullNode.Node.Machine_Id.Value)?.Puid : null,
                    ActualMachineCount = fullNode.Node.Actual_Machine_Count,
                    CalculatedMachineCount = fullNode.Node.Calculated_Machine_Count,
                    CalculatedTargetRate = fullNode.Node.Calculated_Target_Rate,
                    CalculatedActualRate = fullNode.Node.Calculated_Actual_Rate,
                    Modifiers = fullNode.Modifiers
                        .Select(fullModifier => new WorkflowModifierExchange
                        {
                            Puid = projectObjects.Modifiers
                                .FirstOrDefault(m => m.Modifier_Id == fullModifier.Modifier.Modifier_Id)?.Puid ?? throw new ArgumentException($"Modifier with id {fullModifier.Modifier.Modifier_Id} not found in project objects"),
                            Attributes = fullModifier.ModifierAttributes
                                .Select(a => new WorkflowModifierAttributeExchange
                                {
                                    AttributePuid = projectObjects.Attributes.FirstOrDefault(attr => attr.Attribute_Id == a.Attribute_Id)?.Puid ?? throw new ArgumentException($"Attribute with id {a.Attribute_Id} not found in project objects"),
                                    FlatBonus = a.Flat_Bonus,
                                    PercentBonus = a.Percent_Bonus,
                                    MultiplicativeBonus = a.Multiplicative_Bonus
                                })
                                .ToList()
                        })
                        .ToList(),
                    RecipeAttributes = fullNode.RecipeAttributes
                        .Select(a => new AttributeRateRequest
                        {
                            Puid = projectObjects.Attributes.FirstOrDefault(attr => attr.Attribute_Id == a.Attribute_Id)?.Puid ?? throw new ArgumentException($"Attribute with id {a.Attribute_Id} not found in project objects"),
                            Rate = a.Rate
                        })
                        .ToList(),
                    MachineAttributes = fullNode.MachineAttributes
                        .Select(a => new AttributeRateRequest
                        {
                            Puid = projectObjects.Attributes.FirstOrDefault(attr => attr.Attribute_Id == a.Attribute_Id)?.Puid ?? throw new ArgumentException($"Attribute with id {a.Attribute_Id} not found in project objects"),
                            Rate = a.Rate
                        })
                        .ToList()
                };
                response.Nodes.Add(nodeResponse);
            }

            foreach (var edge in chart.Edges)
            {
                var productNode = chart.ProductNodes.FirstOrDefault(pn => pn.Workflow_Product_Node_Id == edge.Product_Node_Id);
                var productPuid = projectObjects.Products.FirstOrDefault(p => p.Product_Id == productNode?.Product_Id)?.Puid ?? throw new ArgumentException($"Product with id {productNode?.Product_Id} not found in project objects");
                var edgeResponse = new WorkflowEdgeResponse
                {
                    ProducerNodePuid = edge.Producer_Node_Id.HasValue ? chart.Nodes.First(n => n.Node.Node_Id == edge.Producer_Node_Id.Value).Node.Puid : null,
                    ConsumerNodePuid = edge.Consumer_Node_Id.HasValue ? chart.Nodes.First(n => n.Node.Node_Id == edge.Consumer_Node_Id.Value).Node.Puid : null,
                    ProductPuid = productPuid,
                    CalculatedFlowRate = edge.Calculated_Flow_Rate,
                    ActualFlowRate = edge.Actual_Flow_Rate
                };
                response.Edges.Add(edgeResponse);
            }

            foreach (var target in chart.Targets)
            {
                var targetResponse = new WorkflowTargetExchange
                {
                    ProductPuid = projectObjects.Products.FirstOrDefault(p => p.Product_Id == target.Product_Id)?.Puid ?? throw new ArgumentException($"Product with id {target.Product_Id} not found in project objects"),
                    TargetRate = target.Target_Rate
                };
                response.Targets.Add(targetResponse);
            }

            foreach (var productNode in chart.ProductNodes)
            {
                var productNodeResponse = new WorkflowProductNodeResponse
                {
                    ProductPuid = projectObjects.Products.FirstOrDefault(p => p.Product_Id == productNode.Product_Id)?.Puid ?? throw new ArgumentException($"Product with id {productNode.Product_Id} not found in project objects"),
                    CalculatedFlowRate = productNode.Calculated_Flow_Rate,
                    ActualFlowRateIn = productNode.Actual_Flow_Rate_In,
                    ActualFlowRateOut = productNode.Actual_Flow_Rate_Out,
                    IsExternal = productNode.Is_External
                };
                response.ProductNodes.Add(productNodeResponse);
            }

            foreach (var preferredRecipe in chart.PreferredRecipes)
            {
                var recipePuid = projectObjects.Recipes.FirstOrDefault(r => r.Recipe_Id == preferredRecipe.Recipe_Id)?.Puid ?? throw new ArgumentException($"Recipe with id {preferredRecipe.Recipe_Id} not found in project objects");
                response.PreferredRecipes.Add(recipePuid);
            }

            return response;
        }
    }
}