using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Helpers;

namespace ProductionCalculator.Business.Services
{
    public class WorkflowChartAssembler : IWorkflowChartAssembler
    {
        private readonly IMachineCalculator _machineCalculator;
        public WorkflowChartAssembler(IMachineCalculator machineCalculator)
        {
            _machineCalculator = machineCalculator;
        }

        public async Task<NodeChart> RebuildChartNodes(NodeChart currentChart, Dictionary<int, double> recipeRates, ProjectObjects projectObjects, Workflow workflow, Func<string, Task<bool>> puidExistsFunc)
        {
            NodeChart updatedChart = new NodeChart
            {
                Nodes = new List<FullNode>(),
                Edges = new List<WorkflowEdge>(),
                ProductNodes = new List<WorkflowProductNode>(),
                Targets = currentChart.Targets,
                PreferredRecipes = currentChart.PreferredRecipes
            };

            // Get recipes that are imported to exclude from nodes
            // Add imported recipes as product nodes and set their Calculated_Flow_Rate based on recipe rates
            var importRecipes = projectObjects.Recipes.Where(r => r.Puid.StartsWith("IMPORT_")).ToList();
            var importRecipeRateDict = importRecipes.ToDictionary(
                r => projectObjects.RecipeProducts.First(rp => rp.Recipe_Id == r.Recipe_Id).Product_Id, 
                r => recipeRates.ContainsKey(r.Recipe_Id) ? recipeRates[r.Recipe_Id] : 0.0
                );

            // Update nodes based on calculated recipe rates
            foreach (var (recipeId, rate) in recipeRates)
            {
                if (importRecipes.Any(r => r.Recipe_Id == recipeId)) continue; // Skip import recipes

                var recipe = projectObjects.Recipes.First(r => r.Recipe_Id == recipeId);

                // Reuse node id if it uses this recipe (still create new object to avoid modifying existing chart)
                var nodeUsingRecipe = currentChart.Nodes.FirstOrDefault(n => n.Node.Recipe_Id == recipeId);
                if (nodeUsingRecipe != null)
                {
                    var machine = nodeUsingRecipe.Node.Machine_Id.HasValue ? projectObjects.Machines.First(m => m.Machine_Id == nodeUsingRecipe.Node.Machine_Id.Value) : null;
                    var updatedNode = new FullNode
                    {
                        Node = nodeUsingRecipe.Node,
                        Modifiers = nodeUsingRecipe.Modifiers
                    };
                    updatedNode.Node.Recipe_Version = recipe.Version;
                    updatedNode.Node.Machine_Id = machine?.Machine_Id;
                    updatedNode.Node.Machine_Version = machine?.Version;
                    updatedNode.Node.Calculated_Target_Rate = rate;
                    updatedNode.Node.Calculated_Machine_Count = machine != null ? _machineCalculator.CalculateMachineCount(rate, recipe, machine, new List<Modifier>()) : null;
                    updatedChart.Nodes.Add(updatedNode);
                }
                else
                {
                    // Generate new puid
                    var puid = await PuidHelper.GenerateUniquePuidAsync(puidExistsFunc);

                    var machine = SelectDefaultMachineForRecipe(recipe, projectObjects);

                    // Create new node for this recipe
                    var newNode = new FullNode
                    {
                        Node = new WorkflowNode
                        {
                            Node_Id = 0, // New node
                            Workflow_Id = workflow.Workflow_Id,
                            Puid = puid,
                            Recipe_Id = recipeId,
                            Recipe_Version = recipe.Version,
                            Machine_Id = machine?.Machine_Id,
                            Machine_Version = machine?.Version,
                            Actual_Machine_Count = null,
                            Calculated_Machine_Count = machine != null ? _machineCalculator.CalculateMachineCount(rate, recipe, machine, new List<Modifier>()) : null,
                            Calculated_Target_Rate = rate,
                            Calculated_Actual_Rate = null
                        },
                        Modifiers = new List<WorkflowNodeModifier>()
                    };
                    updatedChart.Nodes.Add(newNode);
                }
            }
            // Update product nodes
            updatedChart.ProductNodes = AssembleProductNodes(updatedChart, recipeRates, projectObjects);
            // Keep existing product nodes where possible
            // Always keep nodes flagged as external to avoid user defined data loss
            foreach (var productNode in updatedChart.ProductNodes.ToList()) 
            {
                var matchingNode = currentChart.ProductNodes.FirstOrDefault(pn => pn.Product_Id == productNode.Product_Id);
                if (matchingNode != null)
                {
                    if (matchingNode.Is_External)
                    {
                        productNode.Is_External = true;
                        productNode.Calculated_Flow_Rate = importRecipeRateDict[productNode.Product_Id];
                    }
                    productNode.Workflow_Product_Node_Id = matchingNode.Workflow_Product_Node_Id;
                }
            }

            return updatedChart;
        }
        public NodeChart RebuildChartEdges(NodeChart currentChart, NodeChart updatedChart, ProjectObjects projectObjects)
        {
            updatedChart.Edges = AssembleEdges(updatedChart, projectObjects);
            // Keep edges where producer or consumer node is still the same
            // Reduces the amount of IO when updating the chart
            // Set the edge_id to match to keep edge
            foreach (var edge in updatedChart.Edges.ToList())
            {
                // Consumer edge match
                var matchingConsumerEdge = currentChart.Edges.FirstOrDefault(e => e.Consumer_Node_Id == edge.Consumer_Node_Id
                    && e.Product_Node_Id == edge.Product_Node_Id);
                if (matchingConsumerEdge != null)
                {
                    edge.Workflow_Edge_Id = matchingConsumerEdge.Workflow_Edge_Id;
                }
                // Producer edge match
                var matchingProducerEdge = currentChart.Edges.FirstOrDefault(e => e.Producer_Node_Id == edge.Producer_Node_Id
                    && e.Product_Node_Id == edge.Product_Node_Id);
                if (matchingProducerEdge != null)
                {
                    edge.Workflow_Edge_Id = matchingProducerEdge.Workflow_Edge_Id;
                }
            }
            return updatedChart;
        }
        public NodeChart UpdateChartRates(NodeChart chart, Dictionary<int, double> recipeRates, ProjectObjects projectObjects)
        {
            // Update nodes
            foreach (var fullNode in chart.Nodes)
            {
                var recipeId = fullNode.Node.Recipe_Id;
                if (recipeRates.ContainsKey(recipeId))
                {
                    fullNode.Node.Calculated_Actual_Rate = recipeRates[recipeId];
                }
                else
                {
                    fullNode.Node.Calculated_Actual_Rate = 0.0;
                }
            }

            // Update edges
            var productFlowRateIn = new Dictionary<int, double>();
            var productFlowRateOut = new Dictionary<int, double>();
            foreach (var fullNode in chart.Nodes)
            {
                var recipeId = fullNode.Node.Recipe_Id;
                var relatedRecipeProducts = projectObjects.RecipeProducts.Where(rp => rp.Recipe_Id == recipeId);
                foreach (var edge in chart.Edges.Where(e => e.Producer_Node_Id == fullNode.Node.Node_Id || e.Consumer_Node_Id == fullNode.Node.Node_Id))
                {
                    var productNode = chart.ProductNodes.First(pn => pn.Workflow_Product_Node_Id == edge.Product_Node_Id);
                    var rp = relatedRecipeProducts.First(rp => rp.Product_Id == productNode.Product_Id);
                    double flow = rp.Quantity * (recipeRates.ContainsKey(recipeId) ? recipeRates[recipeId] : 0.0);
                    edge.Calculated_Flow_Rate = flow;

                    // Accumulate flow rates for product nodes to update them later
                    if (edge.Producer_Node_Id.HasValue)
                    {
                        if (!productFlowRateOut.ContainsKey(productNode.Workflow_Product_Node_Id))
                        {
                            productFlowRateOut[productNode.Workflow_Product_Node_Id] = 0.0;
                        }
                        productFlowRateOut[productNode.Workflow_Product_Node_Id] += flow;
                    }
                    if (edge.Consumer_Node_Id.HasValue)
                    {
                        if (!productFlowRateIn.ContainsKey(productNode.Workflow_Product_Node_Id))
                        {
                            productFlowRateIn[productNode.Workflow_Product_Node_Id] = 0.0;
                        }
                        productFlowRateIn[productNode.Workflow_Product_Node_Id] += flow;
                    }
                }
            }

            // Update product nodes
            foreach (var productNode in chart.ProductNodes)
            {
                // In
                if (productFlowRateIn.ContainsKey(productNode.Workflow_Product_Node_Id))
                {
                    productNode.Actual_Flow_Rate_In = productFlowRateIn[productNode.Workflow_Product_Node_Id];
                }
                else
                {
                    productNode.Actual_Flow_Rate_In = 0.0;
                }
                // Out
                if (productFlowRateOut.ContainsKey(productNode.Workflow_Product_Node_Id))
                {
                    productNode.Actual_Flow_Rate_Out = productFlowRateOut[productNode.Workflow_Product_Node_Id];
                }
                else
                {
                    productNode.Actual_Flow_Rate_Out = 0.0;
                }
            }
            return chart;
        }

        /// <summary>
        /// Removes persistent components from the node chart that use deleted project objects.
        /// </summary>
        public NodeChart PruneDeletedComponents(NodeChart chart, ProjectObjects projectObjects)
        {
            // Remove targets with deleted products
            chart.Targets = chart.Targets.Where(t => projectObjects.Products.Any(p => p.Product_Id == t.Product_Id)).ToList();

            // Remove external product nodes with deleted products
            // We can remove all outdated product nodes since it will be recalculated anyways
            chart.ProductNodes = chart.ProductNodes.Where(pn => projectObjects.Products.Any(p => p.Product_Id == pn.Product_Id)).ToList();

            // Remove preferred recipes that have been deleted
            chart.PreferredRecipes = chart.PreferredRecipes.Where(pr => projectObjects.Recipes.Any(r => r.Recipe_Id == pr.Recipe_Id)).ToList();

            return chart;
        }

        /// <summary>
        /// Assembles workflow edges based on the node chart and calculated recipe rates.
        /// The nodes and product nodes must already be created in the node chart.
        /// </summary>
        private List<WorkflowEdge> AssembleEdges(NodeChart nodeChart, ProjectObjects projectObjects)
        {
            // Get rate inflow and outflow for each product at each node
            var edgeList = new List<WorkflowEdge>();
            foreach (var fullNode in nodeChart.Nodes)
            {
                var recipeId = fullNode.Node.Recipe_Id;
                var rate = fullNode.Node.Calculated_Target_Rate.GetValueOrDefault(0.0);

                var relatedRecipeProducts = projectObjects.RecipeProducts.Where(rp => rp.Recipe_Id == recipeId);
                foreach (var rp in relatedRecipeProducts)
                {
                    if (rate == 0.0)
                        continue;
                    double flow = rp.Quantity * rate;

                    var productNode = nodeChart.ProductNodes.First(pn => pn.Product_Id == rp.Product_Id);

                    var edge = new WorkflowEdge
                    {
                        Workflow_Edge_Id = 0,
                        Workflow_Id = fullNode.Node.Workflow_Id,
                        Producer_Node_Id = rp.Is_Input ? null : fullNode.Node.Node_Id,
                        Consumer_Node_Id = rp.Is_Input ? fullNode.Node.Node_Id : null,
                        Product_Node_Id = productNode.Workflow_Product_Node_Id,
                        Calculated_Flow_Rate = flow,
                        Actual_Flow_Rate = 0.0
                    };
                    edgeList.Add(edge);
                }
            }
            return edgeList;
        }

        /// <summary>
        /// Assembles workflow product nodes on the node chart and calculated recipe rates.
        /// The nodes must already be created in the node chart.
        /// </summary>
        private List<WorkflowProductNode> AssembleProductNodes(NodeChart nodeChart, Dictionary<int, double> recipeRates, ProjectObjects projectObjects)
        {
            // Get rates of each product from recipes rates
            var productFlowRates = new Dictionary<int, double>();
            foreach (var (recipeId, rate) in recipeRates)
            {
                var relatedRecipeProducts = projectObjects.RecipeProducts.Where(rp => rp.Recipe_Id == recipeId);
                foreach (var rp in relatedRecipeProducts)
                {
                    if (rp.Is_Input && rate == 0.0)
                        continue;
                    double flow = rp.Quantity * rate;
                    if (!productFlowRates.ContainsKey(rp.Product_Id))
                    {
                        productFlowRates[rp.Product_Id] = 0.0;
                    }
                    productFlowRates[rp.Product_Id] += flow;
                }
            }

            var workflowId = nodeChart.Nodes.First().Node.Workflow_Id;

            // Assemble product nodes
            var productNodes = new List<WorkflowProductNode>();
            foreach (var kvp in productFlowRates)
            {
                var productNode = new WorkflowProductNode
                {
                    Workflow_Product_Node_Id = 0, // New node
                    Workflow_Id = workflowId,
                    Product_Id = kvp.Key,
                    Calculated_Flow_Rate = kvp.Value,
                    Actual_Flow_Rate_In = 0.0,
                    Actual_Flow_Rate_Out = 0.0,
                    Is_External = false // Update later when compared to original chart
                };
                productNodes.Add(productNode);
            }
            return productNodes;
        }

        /// <summary>
        /// Selects a default machine for the specified recipe.
        /// If multiple machines can produce the recipe, selects the first one found.
        /// </summary>
        /// <param name="recipe"></param>
        /// <param name="projectObjects"></param>
        /// <returns></returns>
        private Machine? SelectDefaultMachineForRecipe(Recipe recipe, ProjectObjects projectObjects)
        {
            var machineRecipes = projectObjects.MachineRecipes.Where(mr => mr.Recipe_Id == recipe.Recipe_Id).ToList();
            if (!machineRecipes.Any())
            {
                return null; // No machines can produce this recipe
            }
            var machine = projectObjects.Machines.First(m => m.Machine_Id == machineRecipes.First().Machine_Id);
            return machine;
        }
    }
}