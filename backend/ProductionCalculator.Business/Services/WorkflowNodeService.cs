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

        private const double IMPORT_COST = 10000000.0;  // High cost to discourage magic recipes unless necessary
        private const double EXTERNAL_IMPORT = 0.0001; // Very low cost for externally provided products
        private const double DEFAULT_COST = 1.0; // Default cost for recipes

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
        
        public async Task<WorkflowChartResponse> GetWorkflowChartById(Workflow workflow)
		{
			throw new NotImplementedException();
		}

		public async Task<WorkflowChartResponse> UpsertRootDemands(Workflow workflow, List<(string productPuid, double rate)> rootDemands)
		{
			// Get existing chart
            var nodeChart = await GetWorkflowChart(workflow);

            // Update targets
            var projectObjects = await GetProjectObjects(workflow.Project_Id);
            var updatedTargets = new List<WorkflowTarget>();
            foreach (var (productPuid, rate) in rootDemands)
            {
                var product = projectObjects.Products.FirstOrDefault(p => p.Puid == productPuid);
                if (product == null)
                {
                    throw new InvalidOperationException($"Product with PUID {productPuid} not found in project ID {workflow.Project_Id}");
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
            var updatedChart = await CalculateNodeChart(workflow, nodeChart);
            return ConvertToResponse(projectObjects, updatedChart);
		}

		public async Task<WorkflowChartResponse> SetMachine(Workflow workflow, string nodePuid, string machinePuid)
		{
			throw new NotImplementedException();
		}

		public async Task<WorkflowChartResponse> SetRecipe(Workflow workflow, string nodePuid, string? recipePuid)
		{
			throw new NotImplementedException();
		}

		public async Task<WorkflowChartResponse> AddModifier(Workflow workflow, string nodePuid, string modifierPuid)
		{
			throw new NotImplementedException();
		}

		public async Task<WorkflowChartResponse> RemoveModifier(Workflow workflow, string nodePuid, string modifierPuid)
		{
			throw new NotImplementedException();
		}

		public async Task<WorkflowChartResponse> SetActualMachineCount(Workflow workflow, string nodePuid, int actualMachineCount)
		{
			throw new NotImplementedException();
		}

		public async Task<WorkflowChartResponse> SetExternal(Workflow workflow, string nodePuid, bool isExternal)
		{
			throw new NotImplementedException();
		}

		public async Task<WorkflowChartResponse> SetExternalRate(Workflow workflow, string nodePuid, double externalRate)
		{
			throw new NotImplementedException();
		}

        private async Task<NodeChart> GetWorkflowChart(Workflow workflow)
        {
            // Retrieve existing node chart from database
            var nodeChart = await _nodeService.GetByWorkflowId(workflow.Workflow_Id);
            return nodeChart;
        }

        /// <summary>
        /// Takes in a NodeChart, performs necessary calculations, and returns the updated NodeChart.
        /// </summary>
        private async Task<NodeChart> CalculateNodeChart(Workflow workflow, NodeChart nodeChart)
        {
            var projectObjects = await GetProjectObjects(workflow.Project_Id);
            AddImportRecipes(projectObjects, nodeChart);
            var recipeRates = SolveDemand(projectObjects, nodeChart);
            var updatedChart = await UpdateChartDemand(projectObjects, nodeChart, recipeRates, workflow);
            return updatedChart;
        }

        /// <summary>
        /// Updates the node chart based on calculated recipe rates.
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
                Targets = nodeChart.Targets
            };

            // Update nodes based on calculated recipe rates
            foreach (var (recipeId, rate) in recipeRates)
            {
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
                            Is_Preferred = false,
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
                    }
                    productNode.Workflow_Product_Node_Id = matchingNode.Workflow_Product_Node_Id;
                }
            }

            // Nodes, Product_Nodes, and Targets are ready, now save to DB to get primary keys assigned
            updatedChart = await _nodeService.WorkflowNodeAndTargetUpdate(workflow.Workflow_Id, updatedChart);

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
                    Actual_Flow_Rate = 0.0,
                    Is_External = false // Update later when compared to original chart
                };
                productNodes.Add(productNode);
            }
            return productNodes;
        }

        /// <summary>
        /// Sets up and returns a linear solver for demand calculation based on the provided project objects and node chart.
        /// Returns recipe rates as a dictionary mapping Recipe_Id to calculated rate.
        /// </summary>
        private Dictionary<int, double> SolveDemand(ProjectObjects projectObjects, NodeChart nodeChart)
        {
            var recipeVarMap = new Dictionary<int, Variable>();
            var targetDict = nodeChart.Targets.ToDictionary(t => t.Product_Id, t => t.Target_Rate);

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
        /// </summary>
        private bool CheckVersion(NodeChart nodeChart, ProjectObjects projectObjects)
        {
            throw new NotImplementedException();
        }

        private WorkflowChartResponse ConvertToResponse(ProjectObjects projectObjects,NodeChart nodeChart)
        {
            var response = new WorkflowChartResponse
            {
                Nodes = new List<WorkflowNodeResponse>(),
                Edges = new List<WorkflowEdgeResponse>(),
                Targets = new List<WorkflowTargetExchange>(),
                ProductNodes = new List<WorkflowProductNodeResponse>()
            };
            foreach (var fullNode in nodeChart.Nodes)
            {
                var nodeResponse = new WorkflowNodeResponse
                {
                    Puid = fullNode.Node.Puid,
                    RecipePuid = projectObjects.Recipes.First(r => r.Recipe_Id == fullNode.Node.Recipe_Id).Puid,
                    IsPreferred = fullNode.Node.Is_Preferred,
                    MachinePuid = fullNode.Node.Machine_Id.HasValue ? projectObjects.Machines.First(m => m.Machine_Id == fullNode.Node.Machine_Id.Value).Puid : null,
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
                var edgeResponse = new WorkflowEdgeResponse
                {
                    ProducerNodePuid = edge.Producer_Node_Id.HasValue ? nodeChart.Nodes.First(n => n.Node.Node_Id == edge.Producer_Node_Id.Value).Node.Puid : null,
                    ConsumerNodePuid = edge.Consumer_Node_Id.HasValue ? nodeChart.Nodes.First(n => n.Node.Node_Id == edge.Consumer_Node_Id.Value).Node.Puid : null,
                    ProductPuid = projectObjects.Products.First(
                        p => p.Product_Id == nodeChart.ProductNodes.First(
                            pn => pn.Workflow_Product_Node_Id == edge.Product_Node_Id).Product_Id).Puid,
                    CalculatedFlowRate = edge.Calculated_Flow_Rate,
                    ActualFlowRate = edge.Actual_Flow_Rate
                };
                response.Edges.Add(edgeResponse);
            }

            foreach (var target in nodeChart.Targets)
            {
                var targetResponse = new WorkflowTargetExchange
                {
                    ProductPuid = projectObjects.Products.First(p => p.Product_Id == target.Product_Id).Puid,
                    TargetRate = target.Target_Rate
                };
                response.Targets.Add(targetResponse);
            }

            foreach (var productNode in nodeChart.ProductNodes)
            {
                var productNodeResponse = new WorkflowProductNodeResponse
                {
                    ProductPuid = projectObjects.Products.First(p => p.Product_Id == productNode.Product_Id).Puid,
                    CalculatedFlowRate = productNode.Calculated_Flow_Rate,
                    ActualFlowRate = productNode.Actual_Flow_Rate,
                    IsExternal = productNode.Is_External
                };
                response.ProductNodes.Add(productNodeResponse);
            }

            return response;
        }
	}
}
