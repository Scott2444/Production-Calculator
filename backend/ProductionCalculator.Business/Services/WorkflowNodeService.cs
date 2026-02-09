using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Models;
using Google.OrTools.LinearSolver;
using ProductionCalculator.Business.Helpers;


namespace ProductionCalculator.Business.Services
{
    /// <summary>
    /// Service for managing workflow nodes, including retrieval and updates of node charts.
    /// This class is responsible for calculations and operations of node charts.
    /// </summary>
	public class WorkflowNodeService : IWorkflowNodeService
	{
		private readonly IWorkflowNodeDbService _nodeService;
        private readonly IProductRepository _productRepo;
        private readonly IRecipeRepository _recipeRepo;
        private readonly IRecipeProductRepository _recipeProductRepo;
        private readonly IMachineRepository _machineRepo;
        private readonly IMachineRecipeRepository _machineRecipeRepo;
        private readonly IModifierRepository _modifierRepo;
        private readonly IWorkflowNodeRepository _workflowNodeRepo;

        private const double EXTERNAL_IMPORT = 0.0001; // Very low cost for externally provided products
        private const double PREFERRED_RECIPE = 0.01;
        private const double DEFAULT_COST = 1.0; // Default cost for recipes
        private const double TARGET_BONUS = 100000.0; // Bonus to encourage meeting target supply
        private const double OVERFLOW_BONUS = 1000.0; // Bonus to encourage producing this product

		public WorkflowNodeService(
            IWorkflowNodeDbService nodeService, 
            IProductRepository productRepo,
            IRecipeRepository recipeRepo,
            IRecipeProductRepository recipeProductRepo,
            IMachineRepository machineRepo,
            IMachineRecipeRepository machineRecipeRepo,
            IModifierRepository modifierRepo,
            IWorkflowNodeRepository workflowNodeRepo

        )
		{
			_nodeService = nodeService;
            _productRepo = productRepo;
            _recipeRepo = recipeRepo;
            _recipeProductRepo = recipeProductRepo;
            _machineRepo = machineRepo;
            _machineRecipeRepo = machineRecipeRepo;
            _modifierRepo = modifierRepo;
            _workflowNodeRepo = workflowNodeRepo;
		}
        
        public async Task<ServiceResult<WorkflowChartResponse>> GetWorkflowChartById(Workflow workflow)
		{
			var nodeChart = await GetWorkflowChart(workflow);
            var projectObjects = await GetProjectObjects(workflow.Project_Id);
            var response = ConvertToResponse(projectObjects, nodeChart);
            return ServiceResult<WorkflowChartResponse>.SuccessResult(response, ServiceStatus.Ok200);
		}

		public async Task<ServiceResult<WorkflowChartResponse>> UpsertRootDemands(Workflow workflow, List<(string productPuid, double rate)> rootDemands)
		{
			// Get existing chart and project objects
            var nodeChart = await GetWorkflowChart(workflow);
            var projectObjects = await GetProjectObjects(workflow.Project_Id);

            // All demand updates require latest version of objects
            if (!WorkflowIsUpToDate(nodeChart, projectObjects))
            {
                return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.Conflict409, "Node chart is out of date with project data. Please recalculate the node chart to get the latest version.");
            }

            // Upsert targets
            var updatedTargets = new List<WorkflowTarget>();
            foreach (var (productPuid, rate) in rootDemands)
            {
                var product = projectObjects.Products.FirstOrDefault(p => p.Puid == productPuid);
                if (product == null)
                {
                    return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.NotFound404, $"Product with PUID {productPuid} not found in project ID {workflow.Project_Id}");
                }
                var existingTarget = nodeChart.Targets.FirstOrDefault(t => t.Product_Id == product.Product_Id);
                if (existingTarget != null)
                {
                    existingTarget.Target_Rate = rate;
                    updatedTargets.Add(existingTarget);
                }
                else
                {
                    var newTarget = new WorkflowTarget
                    {
                        Workflow_Target_Id = 0, // New target
                        Workflow_Id = workflow.Workflow_Id,
                        Product_Id = product.Product_Id,
                        Target_Rate = rate
                    };
                    updatedTargets.Add(newTarget);
                }
            }
            nodeChart.Targets = updatedTargets;

            // Recalculate chart
            return await SafeCalculateChartAndResponse(workflow, nodeChart, projectObjects);
		}

		public async Task<ServiceResult<WorkflowChartResponse>> UpdateNode(Workflow workflow, string nodePuid, WorkflowNodeRequest request)
		{
			// Get existing chart and project objects
            var nodeChart = await GetWorkflowChart(workflow);
            var projectObjects = await GetProjectObjects(workflow.Project_Id);

            // All demand updates require latest version of objects
            if (!WorkflowIsUpToDate(nodeChart, projectObjects))
            {
                return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.Conflict409, "Node chart is out of date with project data. Please recalculate the node chart to get the latest version.");
            }

            // Find the node to update
            var fullNode = nodeChart.Nodes.FirstOrDefault(n => n.Node.Puid == nodePuid);
            if (fullNode == null)
            {
                return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.NotFound404, $"Node with PUID {nodePuid} not found in workflow ID {workflow.Workflow_Id}");
            }

            // Update node properties based on request
            if (request.MachinePuid != null)
            {
                var machine = projectObjects.Machines.FirstOrDefault(m => m.Puid == request.MachinePuid);
                if (machine == null)
                {
                    return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.NotFound404, $"Machine with PUID {request.MachinePuid} not found in project ID {workflow.Project_Id}");
                }
                fullNode.Node.Machine_Id = machine.Machine_Id;
            }
            fullNode.Modifiers.Clear();
            foreach (var modifierPuid in request.ModifierPuids)
            {
                var modifier = projectObjects.Modifiers.FirstOrDefault(m => m.Puid == modifierPuid);
                if (modifier == null)
                {
                    return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.NotFound404, $"Modifier with PUID {modifierPuid} not found in project ID {workflow.Project_Id}");
                }
                fullNode.Modifiers.Add(new WorkflowNodeModifier
                {
                    Workflow_Node_Modifier_Id = 0, // New node modifier
                    Workflow_Node_Id = fullNode.Node.Node_Id,
                    Modifier_Id = modifier.Modifier_Id,
                    Modifier_Version = modifier.Version
                });
            }
            fullNode.Node.Actual_Machine_Count = request.ActualMachineCount;
            return await SafeCalculateChartAndResponse(workflow, nodeChart, projectObjects, recalculateDemand: false);
		}
        
        public async Task<ServiceResult<WorkflowChartResponse>> SetRecipes(Workflow workflow, List<string> recipePuids)
        {
            // Get existing chart and project objects
            var nodeChart = await GetWorkflowChart(workflow);
            var projectObjects = await GetProjectObjects(workflow.Project_Id);

            // All demand updates require latest version of objects
            if (!WorkflowIsUpToDate(nodeChart, projectObjects))
            {
                return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.Conflict409, "Node chart is out of date with project data. Please recalculate the node chart to get the latest version.");
            }

             // Validate recipes and convert to IDs
            var recipeIds = new List<int>();
            foreach (var recipePuid in recipePuids)
            {
                var recipe = projectObjects.Recipes.FirstOrDefault(r => r.Puid == recipePuid);
                if (recipe == null)
                {
                    return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.NotFound404, $"Recipe with PUID {recipePuid} not found in project ID {workflow.Project_Id}");
                }
                recipeIds.Add(recipe.Recipe_Id);
             }

             // Upsert preferred recipes
             var updatedPreferredRecipes = new List<WorkflowRecipe>();
             foreach (var recipeId in recipeIds)
             {
                var existingPreferredRecipe = nodeChart.PreferredRecipes.FirstOrDefault(pr => pr.Recipe_Id == recipeId);
                if (existingPreferredRecipe != null)
                {
                    updatedPreferredRecipes.Add(existingPreferredRecipe);
                }
                else
                {
                    var newPreferredRecipe = new WorkflowRecipe
                    {
                        Workflow_Recipe_Id = 0, // New preferred recipe
                        Workflow_Id = workflow.Workflow_Id,
                        Recipe_Id = recipeId
                    };
                    updatedPreferredRecipes.Add(newPreferredRecipe);
                }
             }
             nodeChart.PreferredRecipes = updatedPreferredRecipes;

             // Recalculate chart
             return await SafeCalculateChartAndResponse(workflow, nodeChart, projectObjects);
        }

		public async Task<ServiceResult<WorkflowChartResponse>> SetExternal(Workflow workflow, string productPuid, bool isExternal, double? externalRate)
		{
			// Get existing chart and project objects
            var nodeChart = await GetWorkflowChart(workflow);
            var projectObjects = await GetProjectObjects(workflow.Project_Id);

            // All demand updates require latest version of objects
            if (!WorkflowIsUpToDate(nodeChart, projectObjects))
            {
                return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.Conflict409, "Node chart is out of date with project data. Please recalculate the node chart to get the latest version.");
            }

            // Find the product node to update
            var product = projectObjects.Products.FirstOrDefault(p => p.Puid == productPuid);
            if (product == null)
            {
                return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.NotFound404, $"Product with PUID {productPuid} not found in project ID {workflow.Project_Id}");
            }
            var productNode = nodeChart.ProductNodes.FirstOrDefault(pn => pn.Product_Id == product.Product_Id);
            if (productNode == null)
            {
                // Create new product node if it doesn't exist
                var newProductNode = new WorkflowProductNode 
                {
                    Workflow_Product_Node_Id = 0, // New product node
                    Workflow_Id = workflow.Workflow_Id,
                    Product_Id = product.Product_Id,
                    Calculated_Flow_Rate = 0.0,
                    Actual_Flow_Rate_In = externalRate ?? 0.0,
                    Actual_Flow_Rate_Out = 0.0,
                    Is_External = isExternal
                };
                nodeChart.ProductNodes.Add(newProductNode);
            }
            else
            {
                productNode.Actual_Flow_Rate_In = externalRate ?? 0.0;
                productNode.Is_External = isExternal;
            }

            return await SafeCalculateChartAndResponse(workflow, nodeChart, projectObjects);
		}

        public async Task<ServiceResult<WorkflowChartResponse>> UpgradeWorkflowChart(Workflow workflow)
        {
            // Get existing chart and project objects
            var nodeChart = await GetWorkflowChart(workflow);
            var projectObjects = await GetProjectObjects(workflow.Project_Id);

            // Check if chart is already up to date, early exit
            if (WorkflowIsUpToDate(nodeChart, projectObjects))
            {
                var response = ConvertToResponse(projectObjects, nodeChart);
                return ServiceResult<WorkflowChartResponse>.SuccessResult(response, ServiceStatus.Ok200);
            }

            // Keep any user specified values except anything that uses deleted components
            nodeChart = PruneChart(nodeChart, projectObjects);

            return await SafeCalculateChartAndResponse(workflow, nodeChart, projectObjects);
        }

        private async Task<NodeChart> GetWorkflowChart(Workflow workflow)
        {
            // Retrieve existing node chart from database
            var nodeChart = await _nodeService.GetByWorkflowId(workflow.Workflow_Id);
            return nodeChart;
        }

        /// <summary>
        /// Calculates and returns the node chart API response and catches any calculation errors
        /// Used as a wrapper for all operations that update the chart to ensure consistent error handling and response formatting
        /// If recalculateDemand is true, it will recalculate the chart using the demand driven approach which may change the structure of the chart.
        /// </summary>
        private async Task<ServiceResult<WorkflowChartResponse>> SafeCalculateChartAndResponse(Workflow workflow, NodeChart nodeChart, ProjectObjects projectObjects, bool recalculateDemand = true)
        {
            // Recalculate chart
            try 
            {
                var updatedChart = recalculateDemand ? await CalculateNodeChartDS(workflow, nodeChart) : await CalculateNodeChartS(workflow, nodeChart);
                var response = ConvertToResponse(projectObjects, updatedChart);
                return ServiceResult<WorkflowChartResponse>.SuccessResult(response, ServiceStatus.Ok200);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex);
                return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.BadRequest400, $"No possible workflow configuration for the given target demands. {ex.Message}");
            }
        }

        /// <summary>
        /// Calculates the node chart using a demand driven approach where it first ensures all demand is met and then calculates supply based on that demand.
        /// This does not guarantee that the structure will be preserved.
        /// </summary>
        private async Task<NodeChart> CalculateNodeChartDS(Workflow workflow, NodeChart nodeChart)
        {
            var projectObjects = await GetProjectObjects(workflow.Project_Id);
            AddImportRecipes(projectObjects, nodeChart);
            var recipeRates = SolveDemand(projectObjects, nodeChart);
            var updatedChart = await UpdateChartDemand(projectObjects, nodeChart, recipeRates, workflow);
            recipeRates = SolveSupply(projectObjects, updatedChart);
            updatedChart = await UpdateChartSupply(projectObjects, updatedChart, recipeRates, workflow);
            return updatedChart;
        }

        /// <summary>
        /// Calculates the node chart for supply only.
        /// This guarantees that the structure will be preserved.
        /// Only affects the supply calculation.
        /// </summary>
        private async Task<NodeChart> CalculateNodeChartS(Workflow workflow, NodeChart nodeChart)
        {
            var projectObjects = await GetProjectObjects(workflow.Project_Id);
            AddImportRecipes(projectObjects, nodeChart);
            var recipeRates = SolveSupply(projectObjects, nodeChart);
            nodeChart = await UpdateChartSupply(projectObjects, nodeChart, recipeRates, workflow);
            return nodeChart;
        }

        /// <summary>
        /// Structurally updates the node chart based on calculated recipe rates.
        /// Will reuse existing data where possible to minimize database changes and persist user specified values.
        /// First calls DB to update nodes, product nodes, and targets to get the primary keys assigned by DB.
        /// Then calls DB to update edges which depend on those primary keys.
        /// </summary>
        private async Task<NodeChart> UpdateChartDemand(ProjectObjects projectObjects, NodeChart nodeChart, Dictionary<int, double> recipeRates, Workflow workflow)
        {
            NodeChart updatedChart = new NodeChart
            {
                Nodes = new List<FullNode>(),
                Edges = new List<WorkflowEdge>(),
                ProductNodes = new List<WorkflowProductNode>(),
                Targets = nodeChart.Targets,
                PreferredRecipes = nodeChart.PreferredRecipes
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

                // Reuse node if it uses this recipe
                var nodeUsingRecipe = nodeChart.Nodes.FirstOrDefault(n => n.Node.Recipe_Id == recipeId);
                if (nodeUsingRecipe != null)
                {
                    var updatedNode = new FullNode
                    {
                        Node = nodeUsingRecipe.Node,
                        Modifiers = nodeUsingRecipe.Modifiers
                    };
                    updatedNode.Node.Recipe_Version = recipe.Version;
                    updatedNode.Node.Calculated_Target_Rate = rate;
                    updatedNode = CalculateNodeMachineCount(projectObjects, updatedNode);
                    updatedChart.Nodes.Add(updatedNode);
                }
                else
                {
                    // Generate new puid
                    var puid = await PuidHelper.GenerateUniquePuidAsync(_workflowNodeRepo.PuidExists);

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
                            Machine_Id = null,
                            Machine_Version = null,
                            Actual_Machine_Count = null,
                            Calculated_Machine_Count = null,
                            Calculated_Target_Rate = rate,
                            Calculated_Actual_Rate = null
                        },
                        Modifiers = new List<WorkflowNodeModifier>()
                    };
                    newNode = CalculateNodeMachineCount(projectObjects, newNode);
                    updatedChart.Nodes.Add(newNode);
                }
            }
            // Update product nodes
            updatedChart.ProductNodes = AssembleProductNodes(updatedChart, recipeRates, projectObjects);
            // Keep existing product nodes where possible
            // Always keep nodes flagged as external to avoid user defined data loss
            foreach (var productNode in updatedChart.ProductNodes.ToList()) 
            {
                var matchingNode = nodeChart.ProductNodes.FirstOrDefault(pn => pn.Product_Id == productNode.Product_Id);
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

            // Nodes, Product_Nodes, and Targets are ready, now save to DB to get primary keys assigned
            updatedChart = await _nodeService.WorkflowUpdate(workflow.Workflow_Id, updatedChart);

            // Update edges
            updatedChart.Edges = AssembleEdges(updatedChart, recipeRates, projectObjects);
            // Keep edges where producer or consumer node is still the same
            // Reduces the amount of IO when updating the chart
            // Set the edge_id to match to keep edge
            foreach (var edge in updatedChart.Edges.ToList())
            {
                // Consumer edge match
                var matchingConsumerEdge = nodeChart.Edges.FirstOrDefault(e => e.Consumer_Node_Id == edge.Consumer_Node_Id
                    && e.Product_Node_Id == edge.Product_Node_Id);
                if (matchingConsumerEdge != null)
                {
                    edge.Workflow_Edge_Id = matchingConsumerEdge.Workflow_Edge_Id;
                }
                // Producer edge match
                var matchingProducerEdge = nodeChart.Edges.FirstOrDefault(e => e.Producer_Node_Id == edge.Producer_Node_Id
                    && e.Product_Node_Id == edge.Product_Node_Id);
                if (matchingProducerEdge != null)
                {
                    edge.Workflow_Edge_Id = matchingProducerEdge.Workflow_Edge_Id;
                }
            }
            // Now update edges in DB
            updatedChart = await _nodeService.WorkflowEdgeUpdate(workflow.Workflow_Id, updatedChart);

            return updatedChart;
        }


        /// <summary>
        /// Updates the rates on the node chart based on new supply values while keeping the same structure.
        /// Calls Db to update components with new calculated values.
        /// </summary>
        private async Task<NodeChart> UpdateChartSupply(ProjectObjects projectObjects, NodeChart nodeChart, Dictionary<int, double> recipeRates, Workflow workflow)
        {
            // Update nodes
            foreach (var fullNode in nodeChart.Nodes)
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
            foreach (var fullNode in nodeChart.Nodes)
            {
                var recipeId = fullNode.Node.Recipe_Id;
                var relatedRecipeProducts = projectObjects.RecipeProducts.Where(rp => rp.Recipe_Id == recipeId);
                foreach (var edge in nodeChart.Edges.Where(e => e.Producer_Node_Id == fullNode.Node.Node_Id || e.Consumer_Node_Id == fullNode.Node.Node_Id))
                {
                    var productNode = nodeChart.ProductNodes.First(pn => pn.Workflow_Product_Node_Id == edge.Product_Node_Id);
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
            foreach (var productNode in nodeChart.ProductNodes)
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

            // Update chart in DB
            await _nodeService.WorkflowUpdate(workflow.Workflow_Id, nodeChart);
            await _nodeService.WorkflowEdgeUpdate(workflow.Workflow_Id, nodeChart);
            return nodeChart;
        }

        /// <summary>
        /// Calculates the machine count based on target recipe rate, machine, and modifiers.
        /// All units are units per second.
        /// Sets Machine_Id and Machine_Version if not already set.
        /// Sets Calculated_Machine_Count.
        /// </summary>
        private FullNode CalculateNodeMachineCount(ProjectObjects projectObjects, FullNode fullNode)
        {
            // effective_speed =
            // (base_speed + flat_speed_bonus)
            // × (1 + additive_percent_bonus)
            // × multiplicative_modifiers
            // recipes_per_second_per_machine = effective_speed / base_crafting_time<br>
            // machine_count = target_recipe_rate / recipes_per_second_per_machine

            var recipe = projectObjects.Recipes.First(r => r.Recipe_Id == fullNode.Node.Recipe_Id);

            // Base machine speed
            var baseSpeed = 1.0;
            if (fullNode.Node.Machine_Id.HasValue)
            {
                var machine = projectObjects.Machines.FirstOrDefault(m => m.Machine_Id == fullNode.Node.Machine_Id);
                if (machine != null)
                {
                    baseSpeed = machine.Base_Speed;
                }

            }
            else
            {
                // Get first available machine for the recipe
                // If no machine found, use base speed of 1.0 and leave Machine_Id null
                var machineId = projectObjects.MachineRecipes.FirstOrDefault(mr => mr.Recipe_Id == fullNode.Node.Recipe_Id)?.Machine_Id;
                if (machineId.HasValue)
                {
                    var machine = projectObjects.Machines.First(m => m.Machine_Id == machineId);
                    baseSpeed = machine.Base_Speed;
                    fullNode.Node.Machine_Id = machine.Machine_Id;
                    fullNode.Node.Machine_Version = machine.Version;
                }
            }

            // Apply modifiers
            double flatSpeedBonus = 0.0;
            double additivePercentBonus = 0.0;
            double multiplicativeModifier = 1.0;
            foreach (var workflowModifer in fullNode.Modifiers)
            {
                var modifier = projectObjects.Modifiers.FirstOrDefault(m => m.Modifier_Id == workflowModifer.Modifier_Id);
                if (modifier != null)
                {
                    flatSpeedBonus += modifier.Flat_Speed_Bonus;
                    additivePercentBonus += modifier.Additive_Percent_Bonus;
                    multiplicativeModifier *= modifier.Multiplicative_Modifiers;
                }
            }

            // Calculate using formula
            var effective_speed = (baseSpeed + flatSpeedBonus) * (1.0 + additivePercentBonus) * multiplicativeModifier;
            var recipes_per_second_per_machine = effective_speed / recipe.Base_Crafting_Time;
            var machine_count = fullNode.Node.Calculated_Target_Rate / recipes_per_second_per_machine;
            fullNode.Node.Calculated_Machine_Count = machine_count;
            return fullNode;
        }

        /// <summary>
        /// Assembles workflow edges based on the node chart and calculated recipe rates.
        /// The nodes and product nodes must already be created in the node chart.
        /// </summary>
        private List<WorkflowEdge> AssembleEdges(NodeChart nodeChart, Dictionary<int, double> recipeRates, ProjectObjects projectObjects)
        {
            // Get rate inflow and outflow for each product at each node
            var edgeList = new List<WorkflowEdge>();
            foreach (var fullNode in nodeChart.Nodes)
            {
                var recipeId = fullNode.Node.Recipe_Id;
                var rate = recipeRates[recipeId];

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
                    Console.WriteLine($"Assembled edge: Recipe {recipeId}, Product {rp.Product_Id}, Flow {flow}, Producer Node {edge.Producer_Node_Id}, Consumer Node {edge.Consumer_Node_Id}");
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
        /// Uses a linear solver for demand calculation based on the provided project objects and node chart.
        /// Returns recipe rates as a dictionary mapping Recipe_Id to calculated rate.
        /// </summary>
        private Dictionary<int, double> SolveDemand(ProjectObjects projectObjects, NodeChart nodeChart)
        {
            var recipeVarMap = new Dictionary<int, Variable>();
            var targetDict = nodeChart.Targets.ToDictionary(t => t.Product_Id, t => t.Target_Rate);
            var preferredRecipeIds = nodeChart.PreferredRecipes.Select(pr => pr.Recipe_Id).ToHashSet();

            Solver solver = Solver.CreateSolver("GLOP");
            var productConstraintMap = new Dictionary<int, Constraint>();
            var recipeProductNetQuantities = new Dictionary<(int recipeId, int productId), double>();

            // minimize Cost * x
            Objective objective = solver.Objective();
        
            foreach (var recipe in projectObjects.Recipes)
            {
                // x >= 0
                Variable x = solver.MakeNumVar(0.0, double.PositiveInfinity, recipe.Name);
                recipeVarMap[recipe.Recipe_Id] = x;

                // Objective coefficient
                if (recipe.Puid.StartsWith("IMPORT_"))
                {
                    objective.SetCoefficient(x, EXTERNAL_IMPORT);
                    continue;
                }
                // Preferred recipes
                if (preferredRecipeIds.Contains(recipe.Recipe_Id))
                {
                    objective.SetCoefficient(x, PREFERRED_RECIPE);
                    continue;
                }
                objective.SetCoefficient(x, DEFAULT_COST);
            }
            objective.SetMinimization();

            // Create constraints for each product
            // Constraint: (Production - Consumption) >= Demand
            foreach (var product in projectObjects.Products)
            {
                double minDemand = 0.0;
                
                // If this product is a requested target, set the floor to that rate.
                // Otherwise, it is 0 (Intermediate products must not be negative).
                if (targetDict.ContainsKey(product.Product_Id))
                {
                    minDemand = targetDict[product.Product_Id];
                }

                Constraint c = solver.MakeConstraint(minDemand, double.PositiveInfinity, product.Name);
                productConstraintMap[product.Product_Id] = c;
            }

            // Calculate net quantities for recipe-product pairs
            foreach (var rp in projectObjects.RecipeProducts)
            {
                var key = (rp.Recipe_Id, rp.Product_Id);
                if (!recipeProductNetQuantities.ContainsKey(key))
                    recipeProductNetQuantities[key] = 0.0;

                // Inputs are negative flow, Outputs are positive flow
                recipeProductNetQuantities[key] += rp.Quantity * (rp.Is_Input ? -1.0 : 1.0);
            }

            // Fill coefficients by iterating the sparse list
            foreach (var rp in projectObjects.RecipeProducts)
            {
                if (recipeVarMap.ContainsKey(rp.Recipe_Id) && productConstraintMap.ContainsKey(rp.Product_Id))
                {
                    Variable x = recipeVarMap[rp.Recipe_Id];
                    Constraint c = productConstraintMap[rp.Product_Id];
                    
                    // Set the value from the matrix
                    c.SetCoefficient(x, recipeProductNetQuantities[(rp.Recipe_Id, rp.Product_Id)]);
                }
            }

            Solver.ResultStatus resultStatus = solver.Solve();

            if (resultStatus != Solver.ResultStatus.OPTIMAL)
            {
                throw new InvalidOperationException("No optimal solution found for the demand calculation.");
            }

            // TEMPORARY: FOR DEBUGGING PURPOSES ONLY
            Console.WriteLine($"Optimization Successful! Total Cost: {objective.Value()}");
            Console.WriteLine("------------------------------------------------");
            
            // Join back to original Recipe list to get Names
            foreach (var recipe in projectObjects.Recipes)
            {
                var variable = recipeVarMap[recipe.Recipe_Id];
                if (variable.SolutionValue() > 1e-5)
                {
                    Console.WriteLine($"Recipe '{recipe.Name}' (ID {recipe.Recipe_Id}): Run at rate {variable.SolutionValue():F2}");
                }
            }

            // Extract recipe rates
            var recipeRates = new Dictionary<int, double>();
            foreach (var recipe in projectObjects.Recipes)
            {
                var variable = recipeVarMap[recipe.Recipe_Id];
                if (variable.SolutionValue() > 1e-5)
                {
                    recipeRates[recipe.Recipe_Id] = variable.SolutionValue();
                }
            }
            return recipeRates;
        }

        /// <summary>
        /// Uses a linear solver for supply calculation based on only the recipes in the node chart, externally provided products, and Actual_Machine_Count in a node.
        /// Used for calculating supply rates when either the chart structure is updated or when the user updates supply values.
        /// </summary>
        private Dictionary<int, double> SolveSupply(ProjectObjects projectObjects, NodeChart nodeChart)
        {
            var recipeVarMap = new Dictionary<int, Variable>();
            var productConstraintMap = new Dictionary<int, Constraint>();
            var targetDict = nodeChart.Targets.ToDictionary(t => t.Product_Id, t => t.Target_Rate);

            var externalRateByProductId = nodeChart.ProductNodes
                .Where(pn => pn.Is_External)
                .ToDictionary(pn => pn.Product_Id, pn => pn.Actual_Flow_Rate_In);

            var chartRecipeIds = nodeChart.Nodes
                .Select(n => n.Node.Recipe_Id)
                .Distinct()
                .ToHashSet();

            var chartRecipes = projectObjects.Recipes
                .Where(r => chartRecipeIds.Contains(r.Recipe_Id))
                .ToList();

            var importRecipes = projectObjects.Recipes
                .Where(r => r.Puid.StartsWith("IMPORT_"))
                .ToList();

            Solver solver = Solver.CreateSolver("GLOP");
            Objective objective = solver.Objective();

            double GetMaxRecipeRate(FullNode fullNode, Recipe recipe)
            {
                var machineCount = fullNode.Node.Actual_Machine_Count
                    ?? fullNode.Node.Calculated_Machine_Count
                    ?? 0.0;

                if (machineCount <= 0.0)
                {
                    return 0.0;
                }

                var baseSpeed = 1.0;
                if (fullNode.Node.Machine_Id.HasValue)
                {
                    var machine = projectObjects.Machines.FirstOrDefault(m => m.Machine_Id == fullNode.Node.Machine_Id.Value);
                    if (machine != null)
                    {
                        baseSpeed = machine.Base_Speed;
                    }
                }
                else
                {
                    var machineId = projectObjects.MachineRecipes
                        .FirstOrDefault(mr => mr.Recipe_Id == fullNode.Node.Recipe_Id)
                        ?.Machine_Id;
                    if (machineId.HasValue)
                    {
                        var machine = projectObjects.Machines.FirstOrDefault(m => m.Machine_Id == machineId.Value);
                        if (machine != null)
                        {
                            baseSpeed = machine.Base_Speed;
                        }
                    }
                }

                double flatSpeedBonus = 0.0;
                double additivePercentBonus = 0.0;
                double multiplicativeModifier = 1.0;
                foreach (var workflowModifier in fullNode.Modifiers)
                {
                    var modifier = projectObjects.Modifiers.FirstOrDefault(m => m.Modifier_Id == workflowModifier.Modifier_Id);
                    if (modifier != null)
                    {
                        flatSpeedBonus += modifier.Flat_Speed_Bonus;
                        additivePercentBonus += modifier.Additive_Percent_Bonus;
                        multiplicativeModifier *= modifier.Multiplicative_Modifiers;
                    }
                }

                if (recipe.Base_Crafting_Time <= 0.0)
                {
                    return 0.0;
                }

                var effectiveSpeed = (baseSpeed + flatSpeedBonus) * (1.0 + additivePercentBonus) * multiplicativeModifier;
                var recipesPerSecondPerMachine = effectiveSpeed / recipe.Base_Crafting_Time;
                return Math.Max(0.0, recipesPerSecondPerMachine * machineCount);
            }

            // Real recipe variables (bounded by machine capacity)
            foreach (var fullNode in nodeChart.Nodes)
            {
                var recipe = projectObjects.Recipes.First(r => r.Recipe_Id == fullNode.Node.Recipe_Id);
                var maxRate = GetMaxRecipeRate(fullNode, recipe);
                var variable = solver.MakeNumVar(0.0, maxRate, recipe.Name);
                recipeVarMap[recipe.Recipe_Id] = variable;
                objective.SetCoefficient(variable, 1.0);
            }

            // Import recipe variables (bounded by external flow rates)
            foreach (var importRecipe in importRecipes)
            {
                var rp = projectObjects.RecipeProducts.FirstOrDefault(r => r.Recipe_Id == importRecipe.Recipe_Id);
                if (rp == null)
                {
                    continue;
                }

                var maxRate = externalRateByProductId.ContainsKey(rp.Product_Id)
                    ? Math.Max(0.0, externalRateByProductId[rp.Product_Id])
                    : 0.0;

                var variable = solver.MakeNumVar(0.0, maxRate, importRecipe.Name);
                recipeVarMap[importRecipe.Recipe_Id] = variable;
                objective.SetCoefficient(variable, 0.0);
            }

            // Create constraints for each product: Production - Consumption - Sinks >= 0
            foreach (var product in projectObjects.Products)
            {
                var constraint = solver.MakeConstraint(0.0, double.PositiveInfinity, product.Name);
                productConstraintMap[product.Product_Id] = constraint;
            }

            var recipeProductNetQuantities = new Dictionary<(int recipeId, int productId), double>();
            foreach (var rp in projectObjects.RecipeProducts)
            {
                if (!recipeVarMap.ContainsKey(rp.Recipe_Id))
                {
                    continue;
                }

                var key = (rp.Recipe_Id, rp.Product_Id);
                if (!recipeProductNetQuantities.ContainsKey(key))
                {
                    recipeProductNetQuantities[key] = 0.0;
                }

                recipeProductNetQuantities[key] += rp.Quantity * (rp.Is_Input ? -1.0 : 1.0);
            }

            foreach (var kvp in recipeProductNetQuantities)
            {
                var (recipeId, productId) = kvp.Key;
                if (!recipeVarMap.ContainsKey(recipeId) || !productConstraintMap.ContainsKey(productId))
                {
                    continue;
                }

                var variable = recipeVarMap[recipeId];
                var constraint = productConstraintMap[productId];
                constraint.SetCoefficient(variable, kvp.Value);
            }

            // Add primary and overflow sink variables for targets
            foreach (var target in targetDict)
            {
                if (!productConstraintMap.ContainsKey(target.Key))
                {
                    continue;
                }

                var productConstraint = productConstraintMap[target.Key];
                var primarySink = solver.MakeNumVar(0.0, Math.Max(0.0, target.Value), $"SINK_PRIMARY_{target.Key}");
                var overflowSink = solver.MakeNumVar(0.0, double.PositiveInfinity, $"SINK_OVERFLOW_{target.Key}");

                productConstraint.SetCoefficient(primarySink, -1.0);
                productConstraint.SetCoefficient(overflowSink, -1.0);

                objective.SetCoefficient(primarySink, TARGET_BONUS);
                objective.SetCoefficient(overflowSink, OVERFLOW_BONUS);
            }

            objective.SetMaximization();

            Solver.ResultStatus resultStatus = solver.Solve();

            if (resultStatus != Solver.ResultStatus.OPTIMAL)
            {
                throw new InvalidOperationException("No optimal solution found for the supply calculation.");
            }

            var recipeRates = new Dictionary<int, double>();
            foreach (var recipe in chartRecipes)
            {
                if (!recipeVarMap.ContainsKey(recipe.Recipe_Id))
                {
                    continue;
                }

                var value = recipeVarMap[recipe.Recipe_Id].SolutionValue();
                if (value > 1e-5)
                {
                    recipeRates[recipe.Recipe_Id] = value;
                    Console.WriteLine($"Recipe '{recipe.Name}' (ID {recipe.Recipe_Id}): Run at rate {value:F2}");
                }
            }

            return recipeRates;
        }

        /// <summary>
        /// Adds import recipes for all products in the project.
        /// These recipes allow the solver to always have a solution, even if the project is missing raw material recipes.
        /// </summary>
        private void AddImportRecipes(ProjectObjects projectObjects, NodeChart nodeChart)
        {
            var externalProducts = nodeChart.ProductNodes
                .Where(pn => pn.Is_External)
                .Select(pn => projectObjects.Products.First(p => p.Product_Id == pn.Product_Id))
                .ToList();

            foreach (var product in externalProducts)
            {
                Recipe importRecipe = new Recipe
                {
                    Recipe_Id = -product.Product_Id, // Negative ID to avoid conflicts
                    Project_Id = product.Project_Id,
                    Name = $"Import {product.Name}",
                    Puid = $"IMPORT_{product.Puid}", // All following fields are dummy
                    Description = "Auto-generated import recipe",
                    Base_Crafting_Time = 0.0,
                    Version = 1,
                    Created_At = DateTime.UtcNow,
                    Last_Updated = DateTime.UtcNow
                };
                projectObjects.Recipes.Add(importRecipe);

                RecipeProduct rp = new RecipeProduct
                {
                    Recipe_Product_Id = importRecipe.Recipe_Id,
                    Product_Id = product.Product_Id,
                    Recipe_Id = importRecipe.Recipe_Id,
                    Quantity = 1.0, // Imports add 1 unit of the product
                    Is_Input = false
                };
                projectObjects.RecipeProducts.Add(rp);
            }
        }

        private async Task<ProjectObjects> GetProjectObjects(int projectId) {
            // Gather all necessary project objects for calculations
            var products = await _productRepo.GetProductsByProjectId(projectId);
            var recipes = await _recipeRepo.GetByProjectId(projectId);
            var recipeProducts = new List<RecipeProduct>();
            foreach (var recipe in recipes)
            {
                var rProducts = await _recipeProductRepo.GetByRecipeId(recipe.Recipe_Id);
                recipeProducts.AddRange(rProducts);
            }
            var machines = await _machineRepo.GetMachinesByProjectId(projectId);
            var machineRecipes = new List<MachineRecipe>();
            foreach (var machine in machines)
            {
                var mRecipes = await _machineRecipeRepo.GetByMachineId(machine.Machine_Id);
                machineRecipes.AddRange(mRecipes);
            }
            var modifiers = await _modifierRepo.GetModifiersByProjectId(projectId);
            return new ProjectObjects
            {
                Products = products,
                Recipes = recipes,
                RecipeProducts = recipeProducts,
                Machines = machines,
                MachineRecipes = machineRecipes,
                Modifiers = modifiers
            };
        }

        /// <summary>
        /// Checks the nodechart against the project objects to ensure all versions are up to date.
        /// This also serves as a check that all referenced objects still exist (e.g. if a recipe was deleted after the node chart was calculated).
        /// </summary>
        private bool WorkflowIsUpToDate(NodeChart nodeChart, ProjectObjects projectObjects)
        {
            // Nodes - Check that recipe version and machine? version are up to date with latest project version
            foreach (var fullNode in nodeChart.Nodes)
            {
                var recipe = projectObjects.Recipes.FirstOrDefault(r => r.Recipe_Id == fullNode.Node.Recipe_Id);
                if (recipe == null || fullNode.Node.Recipe_Version != recipe.Version)
                {
                    return false;
                }
                if (fullNode.Node.Machine_Id.HasValue)
                {
                    var machine = projectObjects.Machines.FirstOrDefault(m => m.Machine_Id == fullNode.Node.Machine_Id.Value);
                    if (machine == null || fullNode.Node.Machine_Version != machine.Version)
                    {
                        return false;
                    }
                }
                // Modifiers - Check that modifier versions are up to date with latest project version
                foreach (var workflowModifier in fullNode.Modifiers)
                {
                    var modifier = projectObjects.Modifiers.FirstOrDefault(m => m.Modifier_Id == workflowModifier.Modifier_Id);
                    if (modifier == null || workflowModifier.Modifier_Version != modifier.Version)
                    {
                        return false;
                    }
                }
            }

            // Products - Check that product still exists
            foreach (var productNode in nodeChart.ProductNodes)
            {
                var product = projectObjects.Products.FirstOrDefault(p => p.Product_Id == productNode.Product_Id);
                if (product == null)
                {
                    return false;
                }
            }

            // Preferred Recipes - Check that preferred recipe still exists
            foreach (var preferredRecipe in nodeChart.PreferredRecipes)
            {
                var recipe = projectObjects.Recipes.FirstOrDefault(r => r.Recipe_Id == preferredRecipe.Recipe_Id);
                if (recipe == null)
                {
                    return false;
                }
            }

            // Targets and edges do not need to be checked because they are dependent on the nodes and products which are already checked.
            return true;
        }

        /// <summary>
        /// Used to remove parts of the node chart that use deleted objects.
        /// </summary>
        private NodeChart PruneChart(NodeChart nodeChart, ProjectObjects projectObjects)
        {
            // Remove targets with deleted products
            nodeChart.Targets = nodeChart.Targets.Where(t => projectObjects.Products.Any(p => p.Product_Id == t.Product_Id)).ToList();

            // Remove external product nodes with deleted products
            // We can remove all outdated product nodes since it will be recalculated anyways
            nodeChart.ProductNodes = nodeChart.ProductNodes.Where(pn => projectObjects.Products.Any(p => p.Product_Id == pn.Product_Id)).ToList();

            // Remove preferred recipes that have been deleted
            nodeChart.PreferredRecipes = nodeChart.PreferredRecipes.Where(pr => projectObjects.Recipes.Any(r => r.Recipe_Id == pr.Recipe_Id)).ToList();

            return nodeChart;
        }

        /// <summary>
        /// Converts the node chart and related project objects into a response object for the API.
        /// This is version safe where outdated charts can still be converted to a response, but the response will reflect the latest project data
        /// </summary>
        private WorkflowChartResponse ConvertToResponse(ProjectObjects projectObjects,NodeChart nodeChart)
        {
            var response = new WorkflowChartResponse
            {
                Nodes = new List<WorkflowNodeResponse>(),
                Edges = new List<WorkflowEdgeResponse>(),
                Targets = new List<WorkflowTargetExchange>(),
                ProductNodes = new List<WorkflowProductNodeResponse>(),
                PreferredRecipes = new List<string>()
            };
            foreach (var fullNode in nodeChart.Nodes)
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

            foreach (var edge in nodeChart.Edges)
            {
                var productNode = nodeChart.ProductNodes.FirstOrDefault(pn => pn.Workflow_Product_Node_Id == edge.Product_Node_Id);
                var productPuid = projectObjects.Products.FirstOrDefault(p => p.Product_Id == productNode?.Product_Id)?.Puid ?? "0000000000";
                var edgeResponse = new WorkflowEdgeResponse
                {
                    ProducerNodePuid = edge.Producer_Node_Id.HasValue ? nodeChart.Nodes.First(n => n.Node.Node_Id == edge.Producer_Node_Id.Value).Node.Puid : null,
                    ConsumerNodePuid = edge.Consumer_Node_Id.HasValue ? nodeChart.Nodes.First(n => n.Node.Node_Id == edge.Consumer_Node_Id.Value).Node.Puid : null,
                    ProductPuid = productPuid,
                    CalculatedFlowRate = edge.Calculated_Flow_Rate,
                    ActualFlowRate = edge.Actual_Flow_Rate
                };
                response.Edges.Add(edgeResponse);
            }

            foreach (var target in nodeChart.Targets)
            {
                var targetResponse = new WorkflowTargetExchange
                {
                    ProductPuid = projectObjects.Products.FirstOrDefault(p => p.Product_Id == target.Product_Id)?.Puid ?? "0000000000",
                    TargetRate = target.Target_Rate
                };
                response.Targets.Add(targetResponse);
            }

            foreach (var productNode in nodeChart.ProductNodes)
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

            foreach (var preferredRecipe in nodeChart.PreferredRecipes)
            {
                var recipePuid = projectObjects.Recipes.FirstOrDefault(r => r.Recipe_Id == preferredRecipe.Recipe_Id)?.Puid ?? "0000000000";
                response.PreferredRecipes.Add(recipePuid);
            }

            return response;
        }
	}
}
