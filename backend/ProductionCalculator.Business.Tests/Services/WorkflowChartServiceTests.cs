using System.Diagnostics.CodeAnalysis;
using FakeItEasy;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Services;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Records;
using ProductionCalculator.Business.Helpers;

namespace ProductionCalculator.Business.Tests;

[ExcludeFromCodeCoverage]
public class WorkflowChartServiceTests
{
    private readonly IWorkflowChartDataService _chartDataService;
    private readonly IWorkflowSolver _workflowSolver;
    private readonly IProjectDataService _projectDataService;
    private readonly IWorkflowMapper _workflowMapper;
    private readonly IWorkflowChartAssembler _workflowChartAssembler;
    private readonly IWorkflowChartValidator _workflowChartValidator;
    private readonly IWorkflowNodeUpdater _workflowNodeUpdater;
    private readonly WorkflowChartService _sut;

    
    private static Workflow CreateWorkflow() => new()
    {
        Workflow_Id = 1,
        Project_Id = 1,
        Puid = "wf00000001",
        Name = "wf",
        Description = "",
        Created_At = DateTime.UtcNow,
        Last_Updated = DateTime.UtcNow
    };

    private static NodeChart CreateChart() => new()
    {
        Nodes =
        [
            new FullNode
            {
                Node = new WorkflowNode
                {
                    Node_Id = 10,
                    Workflow_Id = 1,
                    Puid = "node000001",
                    Recipe_Id = 100,
                    Recipe_Version = 1,
                    Machine_Id = 200,
                    Machine_Version = 1,
                    Actual_Machine_Count = 1,
                    Calculated_Machine_Count = 1,
                    Calculated_Target_Rate = 1,
                    Calculated_Actual_Rate = 1
                },
                Modifiers =
                [
                    new WorkflowNodeModifier
                    {
                        Workflow_Node_Modifier_Id = 500,
                        Workflow_Node_Id = 10,
                        Modifier_Id = 300,
                        Modifier_Version = 1
                    }
                ]
            }
        ],
        Edges = [],
        Targets = [],
        ProductNodes = [],
        PreferredRecipes = []
    };

    private static ProjectObjects CreateProjectObjects() => new()
    {
        Products = [],
        Attributes =
        [
            new ProjectAttribute
            {
                Attribute_Id = 400,
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
                Recipe_Id = 100,
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
                Machine_Id = 200,
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
                Modifier_Id = 300,
                Project_Id = 1,
                Puid = "mod0000001",
                Name = "ModA",
                Description = "",
                Flat_Bonus = 0,
                Percent_Bonus = 0,
                Multiplicative_Bonus = 1,
                Input_Percent = 0,
                Output_Percent = 0,
                Version = 1,
                Created_At = DateTime.UtcNow,
                Last_Updated = DateTime.UtcNow
            },
            new Modifier
            {
                Modifier_Id = 301,
                Project_Id = 1,
                Puid = "mod0000002",
                Name = "ModYield",
                Description = "",
                Flat_Bonus = 0,
                Percent_Bonus = 0,
                Multiplicative_Bonus = 1,
                Input_Percent = 0.2,
                Output_Percent = 0.1,
                Version = 1,
                Created_At = DateTime.UtcNow,
                Last_Updated = DateTime.UtcNow
            }
        ],
        ModifierAttributes =
        [
            new ModifierAttribute
            {
                Modifier_Attribute_Id = 1,
                Modifier_Id = 301,
                Attribute_Id = 400,
                Flat_Bonus = 1,
                Percent_Bonus = 2,
                Multiplicative_Bonus = 1,
                Version = 1,
                Created_At = DateTime.UtcNow,
                Last_Updated = DateTime.UtcNow
            }
        ]
    };

    private static WorkflowChartResponse EmptyResponse() => new()
    {
        Nodes = [],
        Edges = [],
        Targets = [],
        ProductNodes = [],
        PreferredRecipes = []
    };

    public WorkflowChartServiceTests()
    {
        _chartDataService = A.Fake<IWorkflowChartDataService>();
        _workflowSolver = A.Fake<IWorkflowSolver>();
        _projectDataService = A.Fake<IProjectDataService>();
        _workflowMapper = A.Fake<IWorkflowMapper>();
        _workflowChartAssembler = A.Fake<IWorkflowChartAssembler>();
        _workflowChartValidator = A.Fake<IWorkflowChartValidator>();
        _workflowNodeUpdater = A.Fake<IWorkflowNodeUpdater>();

        _sut = new WorkflowChartService(
            _chartDataService,
            _workflowSolver,
            _projectDataService,
            _workflowMapper,
            _workflowChartAssembler,
            _workflowChartValidator,
            _workflowNodeUpdater
        );
    }

    [Fact]
    public async Task GetWorkflowChartById_MappingSuccess_ReturnsSuccess()
    {
        // Arrange
        var workflow = CreateWorkflow();
        var nodeChart = CreateChart();
        var projectObjects = CreateProjectObjects();
        var response = EmptyResponse();

        A.CallTo(() => _chartDataService.GetByWorkflowId(workflow.Workflow_Id, A<bool>._)).Returns(nodeChart);
        A.CallTo(() => _projectDataService.GetProjectObjects(workflow.Project_Id)).Returns(projectObjects);
        A.CallTo(() => _workflowMapper.ToResponse(projectObjects, nodeChart)).Returns(response);

        // Act
        var result = await _sut.GetWorkflowChartById(workflow);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(ServiceStatus.Ok200, result.Status);
        Assert.Equal(response, result.Data);
    }

    [Fact]
    public async Task GetWorkflowChartById_MappingFail_ReturnsFail()
    {
        // Arrange
        var workflow = CreateWorkflow();
        var nodeChart = CreateChart();
        var projectObjects = CreateProjectObjects();

        A.CallTo(() => _chartDataService.GetByWorkflowId(workflow.Workflow_Id, A<bool>._)).Returns(nodeChart);
        A.CallTo(() => _projectDataService.GetProjectObjects(workflow.Project_Id)).Returns(projectObjects);
        A.CallTo(() => _workflowMapper.ToResponse(projectObjects, nodeChart)).Throws(new Exception("Mapping error"));

        // Act
        var result = await _sut.GetWorkflowChartById(workflow);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceStatus.InternalServerError500, result.Status);
        Assert.Contains("Error mapping workflow chart: Mapping error", result.ErrorMessage!);
    }

    [Fact]
    public async Task UpsertRootDemands_NotUpToDate_ReturnsConflict()
    {
        // Arrange
        var workflow = CreateWorkflow();
        var nodeChart = CreateChart();
        var projectObjects = CreateProjectObjects();
        A.CallTo(() => _chartDataService.GetByWorkflowId(workflow.Workflow_Id, A<bool>._)).Returns(nodeChart);
        A.CallTo(() => _projectDataService.GetProjectObjects(workflow.Project_Id)).Returns(projectObjects);
        A.CallTo(() => _workflowChartValidator.WorkflowIsUpToDate(nodeChart, projectObjects)).Returns(false);

        // Act
        var result = await _sut.UpsertRootDemands(workflow, []);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceStatus.Conflict409, result.Status);
    }

    [Fact]
    public async Task UpsertRootDemands_ProductMissing_ReturnsNotFound()
    {
        // Arrange
        var workflow = CreateWorkflow();
        var nodeChart = CreateChart();
        var projectObjects = CreateProjectObjects();
        A.CallTo(() => _chartDataService.GetByWorkflowId(workflow.Workflow_Id, A<bool>._)).Returns(nodeChart);
        A.CallTo(() => _projectDataService.GetProjectObjects(workflow.Project_Id)).Returns(projectObjects);
        A.CallTo(() => _workflowChartValidator.WorkflowIsUpToDate(nodeChart, projectObjects)).Returns(true);

        // Act
        var result = await _sut.UpsertRootDemands(workflow, [("missing-puid", 10.0)]);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpsertRootDemands_EverythingIsCorrect_ReturnsSuccess()
    {
        // Arrange
        var workflow = CreateWorkflow();
        var nodeChart = CreateChart();
        var projectObjects = CreateProjectObjects();
        projectObjects.Products.Add(new Product 
        { 
            Product_Id = 5, 
            Puid = "prod5",
            Project_Id = 1,
            Name = "P5",
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        });
        
        A.CallTo(() => _chartDataService.GetByWorkflowId(workflow.Workflow_Id, A<bool>._)).Returns(nodeChart);
        A.CallTo(() => _projectDataService.GetProjectObjects(workflow.Project_Id)).Returns(projectObjects);
        A.CallTo(() => _workflowChartValidator.WorkflowIsUpToDate(nodeChart, projectObjects)).Returns(true);
        A.CallTo(() => _workflowSolver.SolveDemand(projectObjects, nodeChart)).Returns(new Dictionary<int, double>());
        A.CallTo(() => _workflowChartAssembler.RebuildChartNodes(A<NodeChart>._, A<Dictionary<int, double>>._, projectObjects, workflow, A<Func<string, Task<bool>>>._)).Returns(nodeChart);
        A.CallTo(() => _workflowChartAssembler.RebuildChartEdges(A<NodeChart>._, A<NodeChart>._, projectObjects)).Returns(nodeChart);
        A.CallTo(() => _workflowSolver.SolveSupply(projectObjects, nodeChart)).Returns(new SolverSupplyResult(new Dictionary<int, double>(), new Dictionary<int, double>(), new Dictionary<int, double>()));
        A.CallTo(() => _workflowChartAssembler.UpdateChartRates(nodeChart, A<SolverSupplyResult>._, projectObjects)).Returns(nodeChart);
        A.CallTo(() => _workflowMapper.ToResponse(projectObjects, nodeChart)).Returns(EmptyResponse());

        // Act
        var result = await _sut.UpsertRootDemands(workflow, [("prod5", 10.0)]);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(ServiceStatus.Ok200, result.Status);
        Assert.Single(nodeChart.Targets);
        Assert.Equal(5, nodeChart.Targets[0].Product_Id);
        Assert.Equal(10.0, nodeChart.Targets[0].Target_Rate);
    }

    [Fact]
    public async Task UpsertRootDemands_ProductAlreadyInTargets_UpdatesExistingTarget()
    {
        // Arrange
        var workflow = CreateWorkflow();
        var nodeChart = CreateChart();
        var projectObjects = CreateProjectObjects();
        projectObjects.Products.Add(new Product 
        { 
            Product_Id = 5, 
            Puid = "prod5",
            Project_Id = 1,
            Name = "P5",
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        });
        nodeChart.Targets.Add(new WorkflowTarget { Workflow_Target_Id = 1, Workflow_Id = 1, Product_Id = 5, Target_Rate = 2.0 });
        
        A.CallTo(() => _chartDataService.GetByWorkflowId(workflow.Workflow_Id, A<bool>._)).Returns(nodeChart);
        A.CallTo(() => _projectDataService.GetProjectObjects(workflow.Project_Id)).Returns(projectObjects);
        A.CallTo(() => _workflowChartValidator.WorkflowIsUpToDate(nodeChart, projectObjects)).Returns(true);
        A.CallTo(() => _workflowSolver.SolveDemand(projectObjects, nodeChart)).Returns(new Dictionary<int, double>());
        A.CallTo(() => _workflowChartAssembler.RebuildChartNodes(A<NodeChart>._, A<Dictionary<int, double>>._, projectObjects, workflow, A<Func<string, Task<bool>>>._)).Returns(nodeChart);
        A.CallTo(() => _workflowChartAssembler.RebuildChartEdges(A<NodeChart>._, A<NodeChart>._, projectObjects)).Returns(nodeChart);
        A.CallTo(() => _workflowSolver.SolveSupply(projectObjects, nodeChart)).Returns(new SolverSupplyResult(new Dictionary<int, double>(), new Dictionary<int, double>(), new Dictionary<int, double>()));
        A.CallTo(() => _workflowChartAssembler.UpdateChartRates(nodeChart, A<SolverSupplyResult>._, projectObjects)).Returns(nodeChart);
        A.CallTo(() => _workflowMapper.ToResponse(projectObjects, nodeChart)).Returns(EmptyResponse());

        // Act
        var result = await _sut.UpsertRootDemands(workflow, [("prod5", 10.0)]);

        // Assert
        Assert.True(result.Success);
        Assert.Single(nodeChart.Targets);
        Assert.Equal(10.0, nodeChart.Targets[0].Target_Rate);
    }

    [Fact]
    public async Task UpdateNode_NodeDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var workflow = CreateWorkflow();
        var nodeChart = CreateChart();
        var projectObjects = CreateProjectObjects();
        A.CallTo(() => _chartDataService.GetByWorkflowId(workflow.Workflow_Id, A<bool>._)).Returns(nodeChart);
        A.CallTo(() => _projectDataService.GetProjectObjects(workflow.Project_Id)).Returns(projectObjects);
        A.CallTo(() => _workflowChartValidator.WorkflowIsUpToDate(nodeChart, projectObjects)).Returns(true);

        // Act
        var result = await _sut.UpdateNode(workflow, "missing-node", new WorkflowNodeRequest 
        { 
            MachinePuid = "", 
            ActualMachineCount = 1, 
            ModifierPuids = [], 
        });

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateNode_NoRecalculationNeeded_ReturnsSuccessWithoutSolving()
    {
        // Arrange
        var workflow = CreateWorkflow();
        var nodeChart = CreateChart();
        var projectObjects = CreateProjectObjects();
        var nodePuid = nodeChart.Nodes[0].Node.Puid;
        var request = new WorkflowNodeRequest 
        { 
            MachinePuid = "", 
            ActualMachineCount = 1, 
            ModifierPuids = [], 
        };
        var impact = new NodeUpdateImpact(false, false);

        A.CallTo(() => _chartDataService.GetByWorkflowId(workflow.Workflow_Id, A<bool>._)).Returns(nodeChart);
        A.CallTo(() => _projectDataService.GetProjectObjects(workflow.Project_Id)).Returns(projectObjects);
        A.CallTo(() => _workflowChartValidator.WorkflowIsUpToDate(nodeChart, projectObjects)).Returns(true);
        A.CallTo(() => _workflowNodeUpdater.ApplyPutUpdate(nodeChart.Nodes[0], request, projectObjects)).Returns(impact);
        A.CallTo(() => _chartDataService.WorkflowUpdate(workflow.Workflow_Id, nodeChart)).Returns(nodeChart);
        A.CallTo(() => _workflowMapper.ToResponse(projectObjects, nodeChart)).Returns(EmptyResponse());

        // Act
        var result = await _sut.UpdateNode(workflow, nodePuid, request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(ServiceStatus.Ok200, result.Status);
        A.CallTo(() => _workflowSolver.SolveSupply(A<ProjectObjects>._, A<NodeChart>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task UpdateNode_RecalculationNeeded_ReturnsSuccessWithSolving()
    {
        // Arrange
        var workflow = CreateWorkflow();
        var nodeChart = CreateChart();
        var projectObjects = CreateProjectObjects();
        var nodePuid = nodeChart.Nodes[0].Node.Puid;
        var request = new WorkflowNodeRequest 
        { 
            MachinePuid = "", 
            ActualMachineCount = 1, 
            ModifierPuids = [], 
        };
        var impact = new NodeUpdateImpact(true, true);

        A.CallTo(() => _chartDataService.GetByWorkflowId(workflow.Workflow_Id, A<bool>._)).Returns(nodeChart);
        A.CallTo(() => _projectDataService.GetProjectObjects(workflow.Project_Id)).Returns(projectObjects);
        A.CallTo(() => _workflowChartValidator.WorkflowIsUpToDate(nodeChart, projectObjects)).Returns(true);
        A.CallTo(() => _workflowNodeUpdater.ApplyPutUpdate(nodeChart.Nodes[0], request, projectObjects)).Returns(impact);
        
        A.CallTo(() => _workflowSolver.SolveDemand(projectObjects, nodeChart)).Returns(new Dictionary<int, double>());
        A.CallTo(() => _workflowChartAssembler.RebuildChartNodes(A<NodeChart>._, A<Dictionary<int, double>>._, projectObjects, workflow, A<Func<string, Task<bool>>>._)).Returns(nodeChart);
        A.CallTo(() => _workflowChartAssembler.RebuildChartEdges(A<NodeChart>._, A<NodeChart>._, projectObjects)).Returns(nodeChart);
        A.CallTo(() => _workflowSolver.SolveSupply(projectObjects, nodeChart)).Returns(new SolverSupplyResult(new Dictionary<int, double>(), new Dictionary<int, double>(), new Dictionary<int, double>()));
        A.CallTo(() => _workflowChartAssembler.UpdateChartRates(nodeChart, A<SolverSupplyResult>._, projectObjects)).Returns(nodeChart);
        A.CallTo(() => _workflowMapper.ToResponse(projectObjects, nodeChart)).Returns(EmptyResponse());

        // Act
        var result = await _sut.UpdateNode(workflow, nodePuid, request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(ServiceStatus.Ok200, result.Status);
        A.CallTo(() => _workflowSolver.SolveDemand(A<ProjectObjects>._, A<NodeChart>._)).MustHaveHappened();
    }

    [Fact]
    public async Task SetRecipes_RecipeMissing_ReturnsNotFound()
    {
        // Arrange
        var workflow = CreateWorkflow();
        var nodeChart = CreateChart();
        var projectObjects = CreateProjectObjects();
        A.CallTo(() => _chartDataService.GetByWorkflowId(workflow.Workflow_Id, A<bool>._)).Returns(nodeChart);
        A.CallTo(() => _projectDataService.GetProjectObjects(workflow.Project_Id)).Returns(projectObjects);
        A.CallTo(() => _workflowChartValidator.WorkflowIsUpToDate(nodeChart, projectObjects)).Returns(true);

        // Act
        var result = await _sut.SetRecipes(workflow, ["missing-recipe"]);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task SetRecipes_RecipesValid_ReturnsSuccess()
    {
        // Arrange
        var workflow = CreateWorkflow();
        var nodeChart = CreateChart();
        var projectObjects = CreateProjectObjects();
        projectObjects.Recipes.Add(new Recipe 
        { 
            Recipe_Id = 50, 
            Puid = "rec50",
            Project_Id = 1,
            Name = "R50",
            Base_Crafting_Time = 1,
            Version = 1,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        });
        
        A.CallTo(() => _chartDataService.GetByWorkflowId(workflow.Workflow_Id, A<bool>._)).Returns(nodeChart);
        A.CallTo(() => _projectDataService.GetProjectObjects(workflow.Project_Id)).Returns(projectObjects);
        A.CallTo(() => _workflowChartValidator.WorkflowIsUpToDate(nodeChart, projectObjects)).Returns(true);
        
        A.CallTo(() => _workflowSolver.SolveDemand(projectObjects, nodeChart)).Returns(new Dictionary<int, double>());
        A.CallTo(() => _workflowChartAssembler.RebuildChartNodes(A<NodeChart>._, A<Dictionary<int, double>>._, projectObjects, workflow, A<Func<string, Task<bool>>>._)).Returns(nodeChart);
        A.CallTo(() => _workflowChartAssembler.RebuildChartEdges(A<NodeChart>._, A<NodeChart>._, projectObjects)).Returns(nodeChart);
        A.CallTo(() => _workflowSolver.SolveSupply(projectObjects, nodeChart)).Returns(new SolverSupplyResult(new Dictionary<int, double>(), new Dictionary<int, double>(), new Dictionary<int, double>()));
        A.CallTo(() => _workflowChartAssembler.UpdateChartRates(nodeChart, A<SolverSupplyResult>._, projectObjects)).Returns(nodeChart);
        A.CallTo(() => _workflowMapper.ToResponse(projectObjects, nodeChart)).Returns(EmptyResponse());

        // Act
        var result = await _sut.SetRecipes(workflow, ["rec50"]);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(ServiceStatus.Ok200, result.Status);
        Assert.Single(nodeChart.PreferredRecipes);
        Assert.Equal(50, nodeChart.PreferredRecipes[0].Recipe_Id);
    }

    [Fact]
    public async Task SetExternal_ProductMissing_ReturnsNotFound()
    {
        // Arrange
        var workflow = CreateWorkflow();
        var nodeChart = CreateChart();
        var projectObjects = CreateProjectObjects();
        A.CallTo(() => _chartDataService.GetByWorkflowId(workflow.Workflow_Id, A<bool>._)).Returns(nodeChart);
        A.CallTo(() => _projectDataService.GetProjectObjects(workflow.Project_Id)).Returns(projectObjects);
        A.CallTo(() => _workflowChartValidator.WorkflowIsUpToDate(nodeChart, projectObjects)).Returns(true);

        // Act
        var result = await _sut.SetExternal(workflow, "missing-prod", true, 10.0);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task SetExternal_SettingNewExternal_ReturnsSuccess()
    {
        // Arrange
        var workflow = CreateWorkflow();
        var nodeChart = CreateChart();
        var projectObjects = CreateProjectObjects();
        projectObjects.Products.Add(new Product 
        { 
            Product_Id = 60, 
            Puid = "prod60",
            Project_Id = 1,
            Name = "P60",
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        });

        A.CallTo(() => _chartDataService.GetByWorkflowId(workflow.Workflow_Id, A<bool>._)).Returns(nodeChart);
        A.CallTo(() => _projectDataService.GetProjectObjects(workflow.Project_Id)).Returns(projectObjects);
        A.CallTo(() => _workflowChartValidator.WorkflowIsUpToDate(nodeChart, projectObjects)).Returns(true);
        
        A.CallTo(() => _workflowSolver.SolveDemand(projectObjects, nodeChart)).Returns(new Dictionary<int, double>());
        A.CallTo(() => _workflowChartAssembler.RebuildChartNodes(A<NodeChart>._, A<Dictionary<int, double>>._, projectObjects, workflow, A<Func<string, Task<bool>>>._)).Returns(nodeChart);
        A.CallTo(() => _workflowChartAssembler.RebuildChartEdges(A<NodeChart>._, A<NodeChart>._, projectObjects)).Returns(nodeChart);
        A.CallTo(() => _workflowSolver.SolveSupply(projectObjects, nodeChart)).Returns(new SolverSupplyResult(new Dictionary<int, double>(), new Dictionary<int, double>(), new Dictionary<int, double>()));
        A.CallTo(() => _workflowChartAssembler.UpdateChartRates(nodeChart, A<SolverSupplyResult>._, projectObjects)).Returns(nodeChart);
        A.CallTo(() => _workflowMapper.ToResponse(projectObjects, nodeChart)).Returns(EmptyResponse());

        // Act
        var result = await _sut.SetExternal(workflow, "prod60", true, 60.0);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(ServiceStatus.Ok200, result.Status);
        Assert.Single(nodeChart.ProductNodes);
        Assert.Equal(60, nodeChart.ProductNodes[0].Product_Id);
        Assert.Equal(60.0, nodeChart.ProductNodes[0].Actual_Flow_Rate_In);
        Assert.True(nodeChart.ProductNodes[0].Is_External);
    }

    [Fact]
    public async Task SetExternal_AlreadyInChart_UpdatesExistingProductNode()
    {
        // Arrange
        var workflow = CreateWorkflow();
        var nodeChart = CreateChart();
        var projectObjects = CreateProjectObjects();
        projectObjects.Products.Add(new Product 
        { 
            Product_Id = 60, 
            Puid = "prod60",
            Project_Id = 1,
            Name = "P60",
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        });
        nodeChart.ProductNodes.Add(new WorkflowProductNode 
        { 
            Workflow_Product_Node_Id = 1, 
            Workflow_Id = 1, 
            Product_Id = 60, 
            Is_External = false, 
            Actual_Flow_Rate_In = 0,
            Calculated_Flow_Rate = 0,
            Actual_Flow_Rate_Out = 0
        });

        A.CallTo(() => _chartDataService.GetByWorkflowId(workflow.Workflow_Id, A<bool>._)).Returns(nodeChart);
        A.CallTo(() => _projectDataService.GetProjectObjects(workflow.Project_Id)).Returns(projectObjects);
        A.CallTo(() => _workflowChartValidator.WorkflowIsUpToDate(nodeChart, projectObjects)).Returns(true);
        
        A.CallTo(() => _workflowSolver.SolveDemand(projectObjects, nodeChart)).Returns(new Dictionary<int, double>());
        A.CallTo(() => _workflowChartAssembler.RebuildChartNodes(A<NodeChart>._, A<Dictionary<int, double>>._, projectObjects, workflow, A<Func<string, Task<bool>>>._)).Returns(nodeChart);
        A.CallTo(() => _workflowChartAssembler.RebuildChartEdges(A<NodeChart>._, A<NodeChart>._, projectObjects)).Returns(nodeChart);
        A.CallTo(() => _workflowSolver.SolveSupply(projectObjects, nodeChart)).Returns(new SolverSupplyResult(new Dictionary<int, double>(), new Dictionary<int, double>(), new Dictionary<int, double>()));
        A.CallTo(() => _workflowChartAssembler.UpdateChartRates(nodeChart, A<SolverSupplyResult>._, projectObjects)).Returns(nodeChart);
        A.CallTo(() => _workflowMapper.ToResponse(projectObjects, nodeChart)).Returns(EmptyResponse());

        // Act
        var result = await _sut.SetExternal(workflow, "prod60", true, 60.0);

        // Assert
        Assert.True(result.Success);
        Assert.Single(nodeChart.ProductNodes);
        Assert.True(nodeChart.ProductNodes[0].Is_External);
        Assert.Equal(60.0, nodeChart.ProductNodes[0].Actual_Flow_Rate_In);
    }

    [Fact]
    public async Task UpgradeWorkflowChart_AlreadyUpToDate_ReturnsSuccess()
    {
        // Arrange
        var workflow = CreateWorkflow();
        var nodeChart = CreateChart();
        var projectObjects = CreateProjectObjects();
        A.CallTo(() => _chartDataService.GetByWorkflowId(workflow.Workflow_Id, A<bool>._)).Returns(nodeChart);
        A.CallTo(() => _projectDataService.GetProjectObjects(workflow.Project_Id)).Returns(projectObjects);
        A.CallTo(() => _workflowChartValidator.WorkflowIsUpToDate(nodeChart, projectObjects)).Returns(true);
        A.CallTo(() => _workflowMapper.ToResponse(projectObjects, nodeChart)).Returns(EmptyResponse());

        // Act
        var result = await _sut.UpgradeWorkflowChart(workflow);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(ServiceStatus.Ok200, result.Status);
        A.CallTo(() => _workflowChartAssembler.PruneDeletedComponents(A<NodeChart>._, A<ProjectObjects>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task UpgradeWorkflowChart_UpgradeNeeded_ReturnsSuccess()
    {
        // Arrange
        var workflow = CreateWorkflow();
        var nodeChart = CreateChart();
        var projectObjects = CreateProjectObjects();
        A.CallTo(() => _chartDataService.GetByWorkflowId(workflow.Workflow_Id, A<bool>._)).Returns(nodeChart);
        A.CallTo(() => _projectDataService.GetProjectObjects(workflow.Project_Id)).Returns(projectObjects);
        A.CallTo(() => _workflowChartValidator.WorkflowIsUpToDate(nodeChart, projectObjects)).Returns(false);
        A.CallTo(() => _workflowChartAssembler.PruneDeletedComponents(nodeChart, projectObjects)).Returns(nodeChart);

        A.CallTo(() => _workflowSolver.SolveDemand(projectObjects, nodeChart)).Returns(new Dictionary<int, double>());
        A.CallTo(() => _workflowChartAssembler.RebuildChartNodes(A<NodeChart>._, A<Dictionary<int, double>>._, projectObjects, workflow, A<Func<string, Task<bool>>>._)).Returns(nodeChart);
        A.CallTo(() => _workflowChartAssembler.RebuildChartEdges(A<NodeChart>._, A<NodeChart>._, projectObjects)).Returns(nodeChart);
        A.CallTo(() => _workflowSolver.SolveSupply(projectObjects, nodeChart)).Returns(new SolverSupplyResult(new Dictionary<int, double>(), new Dictionary<int, double>(), new Dictionary<int, double>()));
        A.CallTo(() => _workflowChartAssembler.UpdateChartRates(nodeChart, A<SolverSupplyResult>._, projectObjects)).Returns(nodeChart);
        A.CallTo(() => _workflowMapper.ToResponse(projectObjects, nodeChart)).Returns(EmptyResponse());

        // Act
        var result = await _sut.UpgradeWorkflowChart(workflow);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(ServiceStatus.Ok200, result.Status);
        A.CallTo(() => _workflowChartAssembler.PruneDeletedComponents(nodeChart, projectObjects)).MustHaveHappened();
    }
}

