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
                    RecipePuid = projectObjects.Recipes.FirstOrDefault(r => r.Recipe_Id == fullNode.Node.Recipe_Id)?.Puid ?? "0000000000",
                    MachinePuid = fullNode.Node.Machine_Id.HasValue ? projectObjects.Machines.FirstOrDefault(m => m.Machine_Id == fullNode.Node.Machine_Id.Value)?.Puid : null,
                    ActualMachineCount = fullNode.Node.Actual_Machine_Count,
                    CalculatedMachineCount = fullNode.Node.Calculated_Machine_Count,
                    CalculatedTargetRate = fullNode.Node.Calculated_Target_Rate,
                    CalculatedActualRate = fullNode.Node.Calculated_Actual_Rate,
                    ModifierPuids = projectObjects.Modifiers
                        .Where(m => fullNode.Modifiers.Any(wm => wm.Modifier_Id == m.Modifier_Id))
                        .Select(m => m.Puid)
                        .ToList()
                };
                response.Nodes.Add(nodeResponse);
            }

            foreach (var edge in chart.Edges)
            {
                var productNode = chart.ProductNodes.FirstOrDefault(pn => pn.Workflow_Product_Node_Id == edge.Product_Node_Id);
                var productPuid = projectObjects.Products.FirstOrDefault(p => p.Product_Id == productNode?.Product_Id)?.Puid ?? "0000000000";
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
                    ProductPuid = projectObjects.Products.FirstOrDefault(p => p.Product_Id == target.Product_Id)?.Puid ?? "0000000000",
                    TargetRate = target.Target_Rate
                };
                response.Targets.Add(targetResponse);
            }

            foreach (var productNode in chart.ProductNodes)
            {
                var productNodeResponse = new WorkflowProductNodeResponse
                {
                    ProductPuid = projectObjects.Products.FirstOrDefault(p => p.Product_Id == productNode.Product_Id)?.Puid ?? "0000000000",
                    CalculatedFlowRate = productNode.Calculated_Flow_Rate,
                    ActualFlowRateIn = productNode.Actual_Flow_Rate_In,
                    ActualFlowRateOut = productNode.Actual_Flow_Rate_Out,
                    IsExternal = productNode.Is_External
                };
                response.ProductNodes.Add(productNodeResponse);
            }

            foreach (var preferredRecipe in chart.PreferredRecipes)
            {
                var recipePuid = projectObjects.Recipes.FirstOrDefault(r => r.Recipe_Id == preferredRecipe.Recipe_Id)?.Puid ?? "0000000000";
                response.PreferredRecipes.Add(recipePuid);
            }

            return response;
        }
    }
}