using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Helpers;


namespace ProductionCalculator.Business.Services
{
    /// <summary>
    /// Service for managing workflow nodes, including retrieval and updates of node charts.
    /// This class is responsible for calculations and operations of node charts.
    /// </summary>
	public class WorkflowChartService : IWorkflowChartService
	{
		private readonly IWorkflowChartDataService _chartDataService;
        private readonly IWorkflowSolver _workflowSolver;
        private readonly IProjectDataService _projectDataService;
        private readonly IWorkflowMapper _workflowMapper;
        private readonly IWorkflowChartAssembler _workflowChartAssembler;
        private readonly IWorkflowChartValidator _workflowChartValidator;

		public WorkflowChartService(
            IWorkflowChartDataService chartDataService, 
            IWorkflowSolver workflowSolver,
            IProjectDataService projectDataService,
            IWorkflowMapper workflowMapper,
            IWorkflowChartAssembler workflowChartAssembler,
            IWorkflowChartValidator workflowChartValidator
        )
		{
			_chartDataService = chartDataService;
            _workflowSolver = workflowSolver;
            _projectDataService = projectDataService;
            _workflowMapper = workflowMapper;
            _workflowChartAssembler = workflowChartAssembler;
            _workflowChartValidator = workflowChartValidator;
		}
        
        public async Task<ServiceResult<WorkflowChartResponse>> GetWorkflowChartById(Workflow workflow)
		{
			var nodeChart = await GetWorkflowChart(workflow);
            var projectObjects = await _projectDataService.GetProjectObjects(workflow.Project_Id);
            var response = _workflowMapper.ToResponse(projectObjects, nodeChart);
            return ServiceResult<WorkflowChartResponse>.SuccessResult(response, ServiceStatus.Ok200);
		}

		public async Task<ServiceResult<WorkflowChartResponse>> UpsertRootDemands(Workflow workflow, List<(string productPuid, double rate)> rootDemands)
		{
			// Get existing chart and project objects
            var nodeChart = await GetWorkflowChart(workflow);
            var projectObjects = await _projectDataService.GetProjectObjects(workflow.Project_Id);

            // All demand updates require latest version of objects
            if (!_workflowChartValidator.WorkflowIsUpToDate(nodeChart, projectObjects))
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
            var projectObjects = await _projectDataService.GetProjectObjects(workflow.Project_Id);

            // All demand updates require latest version of objects
            if (!_workflowChartValidator.WorkflowIsUpToDate(nodeChart, projectObjects))
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
            var projectObjects = await _projectDataService.GetProjectObjects(workflow.Project_Id);

            // All demand updates require latest version of objects
            if (!_workflowChartValidator.WorkflowIsUpToDate(nodeChart, projectObjects))
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
            var projectObjects = await _projectDataService.GetProjectObjects(workflow.Project_Id);

            // All demand updates require latest version of objects
            if (!_workflowChartValidator.WorkflowIsUpToDate(nodeChart, projectObjects))
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
            var projectObjects = await _projectDataService.GetProjectObjects(workflow.Project_Id);

            // Check if chart is already up to date, early exit
            if (_workflowChartValidator.WorkflowIsUpToDate(nodeChart, projectObjects))
            {
                var response = _workflowMapper.ToResponse(projectObjects, nodeChart);
                return ServiceResult<WorkflowChartResponse>.SuccessResult(response, ServiceStatus.Ok200);
            }

            // Keep any user specified values except anything that uses deleted components
            nodeChart = _workflowChartAssembler.PruneDeletedComponents(nodeChart, projectObjects);

            return await SafeCalculateChartAndResponse(workflow, nodeChart, projectObjects);
        }

        private async Task<NodeChart> GetWorkflowChart(Workflow workflow)
        {
            // Retrieve existing node chart from database
            var nodeChart = await _chartDataService.GetByWorkflowId(workflow.Workflow_Id);
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
                var updatedChart = recalculateDemand ? await CalculateNodeChartDS(workflow, nodeChart, projectObjects) : await CalculateNodeChartS(nodeChart, projectObjects);
                var response = _workflowMapper.ToResponse(projectObjects, updatedChart);
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
        private async Task<NodeChart> CalculateNodeChartDS(Workflow workflow, NodeChart nodeChart, ProjectObjects projectObjects)
        {
            // Demand calculation
            var recipeRates = _workflowSolver.SolveDemand(projectObjects, nodeChart);
            var updatedChart = await _workflowChartAssembler.RebuildChartNodes(nodeChart, recipeRates, projectObjects, workflow, _chartDataService.NodePuidExists);
            await _chartDataService.WorkflowUpdate(workflow.Workflow_Id, updatedChart); // Update chart in DB to assign IDs before rebuilding edges
            updatedChart = _workflowChartAssembler.RebuildChartEdges(nodeChart, updatedChart, projectObjects);
            // Supply calculation
            recipeRates = _workflowSolver.SolveSupply(projectObjects, updatedChart);
            updatedChart = _workflowChartAssembler.UpdateChartRates(updatedChart, recipeRates, projectObjects);

            await _chartDataService.WorkflowUpdate(workflow.Workflow_Id, updatedChart); // Final update to save all calculated values
            return updatedChart;
        }

        /// <summary>
        /// Calculates the node chart for supply only.
        /// This guarantees that the structure will be preserved.
        /// Only affects the supply calculation.
        /// </summary>
        private async Task<NodeChart> CalculateNodeChartS(NodeChart nodeChart, ProjectObjects projectObjects)
        {
            var recipeRates = _workflowSolver.SolveSupply(projectObjects, nodeChart);
            nodeChart = _workflowChartAssembler.UpdateChartRates(nodeChart, recipeRates, projectObjects);
            return nodeChart;
        }
	}
}
