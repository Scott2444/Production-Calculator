using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Helpers;
using ProductionCalculator.Business.Records;


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
        private readonly IWorkflowNodeUpdater _workflowNodeUpdater;

		public WorkflowChartService(
            IWorkflowChartDataService chartDataService, 
            IWorkflowSolver workflowSolver,
            IProjectDataService projectDataService,
            IWorkflowMapper workflowMapper,
            IWorkflowChartAssembler workflowChartAssembler,
            IWorkflowChartValidator workflowChartValidator,
            IWorkflowNodeUpdater workflowNodeUpdater
        )
		{
			_chartDataService = chartDataService;
            _workflowSolver = workflowSolver;
            _projectDataService = projectDataService;
            _workflowMapper = workflowMapper;
            _workflowChartAssembler = workflowChartAssembler;
            _workflowChartValidator = workflowChartValidator;
            _workflowNodeUpdater = workflowNodeUpdater;
		}
        
        /// <summary>
        /// Retrieves the workflow chart for a given workflow and maps it to a response object for the API.
        /// Does not perform any edits or calculations.
        /// </summary>
        public async Task<ServiceResult<WorkflowChartResponse>> GetWorkflowChartById(Workflow workflow)
		{
			var nodeChart = await _chartDataService.GetByWorkflowId(workflow.Workflow_Id);
            var projectObjects = await _projectDataService.GetProjectObjects(workflow.Project_Id);
            try
            {
                var response = _workflowMapper.ToResponse(projectObjects, nodeChart);
                return ServiceResult<WorkflowChartResponse>.SuccessResult(response, ServiceStatus.Ok200);
            }
            catch (Exception ex)
            {
                return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.InternalServerError500, $"Error mapping workflow chart: {ex.Message}");
            }
		}

        /// <summary>
        /// Calculates the workflow chart based on the provided root demands.
        /// Updates the chart with the new targets and recalculates the entire chart to reflect changes in supply and demand.
        /// This will persist any existing chart changes if the recipe is used again.
        /// Yield modifiers to recipes will still be added to the demand calculations.
        /// </summary>
        /// <param name="workflow"></param>
        /// <param name="rootDemands"></param>
        /// <returns></returns>
		public async Task<ServiceResult<WorkflowChartResponse>> UpsertRootDemands(Workflow workflow, List<(string productPuid, double rate)> rootDemands)
		{
			// Get existing chart and project objects
            var nodeChart = await _chartDataService.GetByWorkflowId(workflow.Workflow_Id);
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
            var nodeChart = await _chartDataService.GetByWorkflowId(workflow.Workflow_Id);
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

            // Update the full node with the new values and determine the impact of the changes
            var impact = _workflowNodeUpdater.ApplyPutUpdate(fullNode, request, projectObjects);

            // If no demand or supply recalculation is needed, we can persist the changes and return early without solving
            if (!impact.RequiresDemandRecalculation && !impact.RequiresSupplyRecalculation)
            {
                var updatedChart = await _chartDataService.WorkflowUpdate(workflow.Workflow_Id, nodeChart);
                var response = _workflowMapper.ToResponse(projectObjects, updatedChart);
                return ServiceResult<WorkflowChartResponse>.SuccessResult(response, ServiceStatus.Ok200);
            }

            // 3. Recalculate if necessary
            return await SafeCalculateChartAndResponse(workflow, nodeChart, projectObjects, recalculateDemand: impact.RequiresDemandRecalculation);
        }
        
        public async Task<ServiceResult<WorkflowChartResponse>> SetRecipes(Workflow workflow, List<string> recipePuids)
        {
            // Get existing chart and project objects
            var nodeChart = await _chartDataService.GetByWorkflowId(workflow.Workflow_Id);
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
            var nodeChart = await _chartDataService.GetByWorkflowId(workflow.Workflow_Id);
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
            var nodeChart = await _chartDataService.GetByWorkflowId(workflow.Workflow_Id);
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
                var updatedChart = recalculateDemand ? 
                    await CalculateNodeChartDS(workflow, nodeChart, projectObjects) : 
                    await CalculateNodeChartS(workflow, nodeChart, projectObjects);
                var response = _workflowMapper.ToResponse(projectObjects, updatedChart);
                return ServiceResult<WorkflowChartResponse>.SuccessResult(response, ServiceStatus.Ok200);
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.BadRequest400, $"No possible workflow configuration for the given target demands. {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.InternalServerError500, $"Error calculating workflow chart: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculates the node chart using a demand driven approach where it first ensures all demand is met and then calculates supply based on that demand.
        /// This does not guarantee that the structure will be preserved.
        /// Saves changes to database.
        /// </summary>
        private async Task<NodeChart> CalculateNodeChartDS(Workflow workflow, NodeChart nodeChart, ProjectObjects projectObjects)
        {
            // Demand calculation
            var recipeRates = _workflowSolver.SolveDemand(projectObjects, nodeChart);
            var updatedChart = await _workflowChartAssembler.RebuildChartNodes(nodeChart, recipeRates, projectObjects, workflow, _chartDataService.NodePuidExists);
            await _chartDataService.WorkflowUpdate(workflow.Workflow_Id, updatedChart); // Update chart in DB to assign node IDs before rebuilding edges
            updatedChart = _workflowChartAssembler.RebuildChartEdges(nodeChart, updatedChart, projectObjects);
            // Supply calculation
            var supplySolverResult = _workflowSolver.SolveSupply(projectObjects, updatedChart);
            updatedChart = _workflowChartAssembler.UpdateChartRates(updatedChart, supplySolverResult, projectObjects);

            await _chartDataService.WorkflowUpdate(workflow.Workflow_Id, updatedChart); // Final update to save all calculated values
            return updatedChart;
        }

        /// <summary>
        /// Calculates the node chart for supply only.
        /// This guarantees that the structure will be preserved.
        /// Only affects the supply calculation.
        /// Saves changes to database.
        /// </summary>
        private async Task<NodeChart> CalculateNodeChartS(Workflow workflow, NodeChart nodeChart, ProjectObjects projectObjects)
        {
            var recipeRates = _workflowSolver.SolveSupply(projectObjects, nodeChart);
            nodeChart = _workflowChartAssembler.UpdateChartRates(nodeChart, recipeRates, projectObjects);
            await _chartDataService.WorkflowUpdate(workflow.Workflow_Id, nodeChart);
            return nodeChart;
        }
	}
}
