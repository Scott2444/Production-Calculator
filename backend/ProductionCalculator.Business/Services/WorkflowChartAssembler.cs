using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Helpers;
using ProductionCalculator.Business.Records;

namespace ProductionCalculator.Business.Services
{
    public class WorkflowChartAssembler : IWorkflowChartAssembler
    {
        private readonly IMachineCalculator _machineCalculator;
        public WorkflowChartAssembler(IMachineCalculator machineCalculator)
        {
            _machineCalculator = machineCalculator;
        }

        /// <summary>
        /// Rebuilds the node chart with updated nodes based on the calculated recipe rates.
        /// All entities will be reused if possible to preserve user defined data.
        /// This method may create or delete nodes if necessary to match the calculated recipe rates.
        /// </summary>
        /// <param name="currentChart">Previous node chart</param>
        /// <param name="recipeRates">Demand recipe rates from workflow solver</param>
        /// <param name="projectObjects">Project entities</param>
        /// <param name="workflow"></param>
        /// <param name="puidExistsFunc">PuidExistsFunction to query database of existing node puids</param>
        /// <returns>New updated node chart</returns>
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
                // Get modifiers for this node if it exists
                var modifiers = new List<WorkflowNodeModifier>();
                foreach (var modifier in nodeUsingRecipe?.Modifiers ?? [])
                {
                    var projectModifier = projectObjects.Modifiers.FirstOrDefault(m => m.Modifier_Id == modifier.Modifier_Id);
                    if (projectModifier != null)
                    {
                        modifier.Modifier_Version = projectModifier.Version;
                        modifiers.Add(modifier);
                    }
                }


                if (nodeUsingRecipe != null)
                {
                    var machine = nodeUsingRecipe.Node.Machine_Id.HasValue ? projectObjects.Machines.First(m => m.Machine_Id == nodeUsingRecipe.Node.Machine_Id.Value) : null;
                    var updatedNode = new FullNode
                    {
                        Node = nodeUsingRecipe.Node,
                        Modifiers = modifiers
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
                        Modifiers = []
                    };
                    updatedChart.Nodes.Add(newNode);
                }
            }
            // Update product nodes
            updatedChart.ProductNodes = AssembleProductNodes(workflow.Workflow_Id, recipeRates, projectObjects);
            // Keep existing product nodes where possible
            foreach (var productNode in updatedChart.ProductNodes)
            {
                var matchingNode = currentChart.ProductNodes.FirstOrDefault(pn => pn.Product_Id == productNode.Product_Id);
                if (matchingNode != null)
                {
                    productNode.Workflow_Product_Node_Id = matchingNode.Workflow_Product_Node_Id;
                }
            }
            // Always keep nodes flagged as external to avoid user defined data loss
            foreach (var productNode in currentChart.ProductNodes.Where(pn => pn.Is_External)) 
            {
                var matchingNode = updatedChart.ProductNodes.FirstOrDefault(pn => pn.Product_Id == productNode.Product_Id);
                if (matchingNode != null)
                {
                    matchingNode.Workflow_Product_Node_Id = productNode.Workflow_Product_Node_Id;
                    matchingNode.Workflow_Id = productNode.Workflow_Id;
                    matchingNode.Product_Id = productNode.Product_Id;
                    matchingNode.Is_External = true;
                    matchingNode.Actual_Flow_Rate_In = productNode.Actual_Flow_Rate_In;
                }
                else
                {
                    updatedChart.ProductNodes.Add(productNode);
                }
            }

            // Update target rates on product nodes
            // Usually, this is set by AssembleProductNodes based on the calculated recipe rates,
            // but when the target is an external product, the target rate must be set here
            foreach (var target in updatedChart.Targets)
            {
                var matchingProductNode = updatedChart.ProductNodes.FirstOrDefault(pn => pn.Product_Id == target.Product_Id);
                if (matchingProductNode != null)
                {
                    matchingProductNode.Calculated_Flow_Rate = target.Target_Rate;
                }
                else
                {
                    throw new Exception($"No matching product node found for target with product ID {target.Product_Id} when rebuilding chart nodes.");
                }
            }

            return updatedChart;
        }

        /// <summary>
        /// Builds the edges between nodes after the nodes have been updated with RebuildChartNodes. 
        /// Will reuse edges where possible to reduce IO (and preserve user defined data, although no user defined data is defined in the edges right now).
        /// The DB must assigned node ids before calling this method since the edges require node ids to be assigned.
        /// </summary>
        /// <param name="currentChart"></param>
        /// <param name="updatedChart"></param>
        /// <param name="projectObjects"></param>
        /// <returns>Updated node chart</returns>
        public NodeChart RebuildChartEdges(NodeChart currentChart, NodeChart updatedChart, ProjectObjects projectObjects)
        {
            updatedChart.Edges = AssembleEdges(updatedChart, projectObjects);
            // Keep edges where producer or consumer node is still the same
            // Reduces the amount of IO when updating the chart
            // Set the edge_id to match to keep edge
            foreach (var edge in updatedChart.Edges.ToList())
            {
                // Consumer edge match
                var matchingConsumerEdge = currentChart.Edges.FirstOrDefault(e => 
                    e.Consumer_Node_Id != null && 
                    e.Consumer_Node_Id == edge.Consumer_Node_Id && 
                    e.Product_Node_Id == edge.Product_Node_Id);
                if (matchingConsumerEdge != null)
                {
                    edge.Workflow_Edge_Id = matchingConsumerEdge.Workflow_Edge_Id;
                }
                // Producer edge match
                var matchingProducerEdge = currentChart.Edges.FirstOrDefault(e => 
                    e.Producer_Node_Id != null && 
                    e.Producer_Node_Id == edge.Producer_Node_Id &&
                    e.Product_Node_Id == edge.Product_Node_Id);
                if (matchingProducerEdge != null)
                {
                    edge.Workflow_Edge_Id = matchingProducerEdge.Workflow_Edge_Id;
                }
            }
            return updatedChart;
        }

        /// <summary>
        /// Updates the node chart with calculated recipe rates. This should be used after RebuildChartNodes.
        /// Updates the Calculated_Actual_Rate for nodes
        /// Updates Actual_Flow_Rate for edges and product nodes based on the new recipe rates.
        /// Calculations include the modifier effects on machine count
        /// </summary>
        /// <param name="chart">Updated node chart</param>
        /// <param name="recipeRates">Supply recipe rates from workflow solver</param>
        /// <param name="projectObjects">Project objects</param>
        /// <returns>Updated node chart</returns>
        public NodeChart UpdateChartRates(NodeChart chart, SolverSupplyResult solverSupplyResult, ProjectObjects projectObjects)
        {
            var recipeRates = solverSupplyResult.RecipeRates;

            // Update nodes
            foreach (var fullNode in chart.Nodes)
            {
                var recipeId = fullNode.Node.Recipe_Id;
                // Recalculate machine count since modifiers may have changed since RebuildChartNotes()
                var recipe = projectObjects.Recipes.FirstOrDefault(r => r.Recipe_Id == recipeId);
                fullNode.Node.Calculated_Machine_Count = fullNode.Node.Machine_Id.HasValue 
                        ? _machineCalculator.CalculateMachineCount(
                            fullNode.Node.Calculated_Target_Rate.GetValueOrDefault(0.0), 
                            projectObjects.Recipes.First(r => r.Recipe_Id == recipeId), 
                            projectObjects.Machines.First(m => m.Machine_Id == fullNode.Node.Machine_Id.Value), 
                            fullNode.Modifiers.Select(m => projectObjects.Modifiers.First(mod => mod.Modifier_Id == m.Modifier_Id)).ToList()
                            )
                        : null;
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
            var productFlowRateIn = new Dictionary<int, double>(); // Used for updating product nodes later
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
                    edge.Actual_Flow_Rate = flow;

                    // Accumulate flow rates for product nodes to update them later
                    if (edge.Producer_Node_Id.HasValue)
                    {
                        if (!productFlowRateIn.ContainsKey(productNode.Workflow_Product_Node_Id))
                        {
                            productFlowRateIn[productNode.Workflow_Product_Node_Id] = 0.0;
                        }
                        productFlowRateIn[productNode.Workflow_Product_Node_Id] += flow;
                    }
                    if (edge.Consumer_Node_Id.HasValue)
                    {
                        if (!productFlowRateOut.ContainsKey(productNode.Workflow_Product_Node_Id))
                        {
                            productFlowRateOut[productNode.Workflow_Product_Node_Id] = 0.0;
                        }
                        productFlowRateOut[productNode.Workflow_Product_Node_Id] += flow;
                    }
                }
            }

            // Update product nodes
            var productInFlowRates = solverSupplyResult.ProductInFlowRates;
            var productOutFlowRates = solverSupplyResult.ProductOutFlowRates;
            foreach (var productNode in chart.ProductNodes)
            {
                var isTarget = chart.Targets.Any(t => t.Product_Id == productNode.Product_Id);
                // Special cases for external and target product nodes
                if (productNode.Is_External || isTarget)
                {
                    if (isTarget)
                    {
                        // For target product nodes, the flow rate in is determined by the solver supply using "sink recipe"
                        // Actual flow rate out is the flow from the solver supply result
                        productNode.Actual_Flow_Rate_Out = productOutFlowRates.ContainsKey(productNode.Product_Id) ? productOutFlowRates[productNode.Product_Id] : 0.0;
                    }
                    if (productNode.Is_External)
                    {
                        // For external nodes, the flow rate out is determined by the solver supply using "import recipe"
                        // The actual flow rate in is determined by the user and should not be overwritten
                        // Actual flow rate out is the flow from the solver supply result
                        productNode.Actual_Flow_Rate_Out = productInFlowRates.ContainsKey(productNode.Product_Id) ? productInFlowRates[productNode.Product_Id] : 0.0;
                    }
                    if (isTarget && !productNode.Is_External)
                    {
                        // We cannot overwrite the user provided actual flow rate in for external nodes
                        // Targets still need to have their actual flow rate in updated based on the solver supply result
                        productNode.Actual_Flow_Rate_In = productFlowRateIn.TryGetValue(productNode.Workflow_Product_Node_Id, out double value) ? value : 0.0;
                    }
                }
                else
                {
                    // In
                    productNode.Actual_Flow_Rate_In = productFlowRateIn.TryGetValue(productNode.Workflow_Product_Node_Id, out double valueIn) ? valueIn : 0.0;
                    // Out
                    productNode.Actual_Flow_Rate_Out = productFlowRateOut.TryGetValue(productNode.Workflow_Product_Node_Id, out double valueOut) ? valueOut : 0.0;
                }
            }
            return chart;
        }

        /// <summary>
        /// Removes persistent components from the node chart that use deleted project objects.
        /// Does not delete preferred_recipes since they are user defined and persist even if the recipe is deleted.
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
        private List<WorkflowProductNode> AssembleProductNodes(int workflowId, Dictionary<int, double> recipeRates, ProjectObjects projectObjects)
        {
            // Get rates of each product from recipes rates
            var productFlowRates = new Dictionary<int, double>();
            foreach (var (recipeId, rate) in recipeRates)
            {
                var relatedRecipeProducts = projectObjects.RecipeProducts.Where(rp => rp.Recipe_Id == recipeId);
                foreach (var rp in relatedRecipeProducts)
                {
                    // Only calculate flow from outputs of recipes
                    if (rp.Is_Input)
                        continue;
                    double flow = rp.Quantity * rate;
                    if (!productFlowRates.ContainsKey(rp.Product_Id))
                    {
                        productFlowRates[rp.Product_Id] = 0.0;
                    }
                    productFlowRates[rp.Product_Id] += flow;
                }
            }

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