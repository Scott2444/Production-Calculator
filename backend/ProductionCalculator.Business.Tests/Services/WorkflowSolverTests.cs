using System.Diagnostics.CodeAnalysis;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Services;

namespace ProductionCalculator.Business.Tests;

[ExcludeFromCodeCoverage]
public class WorkflowSolverTests
{
	private static readonly DateTime StaticNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	private static Product Product(int id, string name)
	{
		return new Product
		{
			Product_Id = id,
			Project_Id = 1,
			Puid = $"p{id}",
			Name = name,
			Created_At = StaticNow,
			Last_Updated = StaticNow
		};
	}

	private static Recipe Recipe(int id, string name)
	{
		return new Recipe
		{
			Recipe_Id = id,
			Project_Id = 1,
			Puid = $"r{id}",
			Name = name,
			Base_Crafting_Time = 1.0,
			Version = 1,
			Created_At = StaticNow,
			Last_Updated = StaticNow
		};
	}

	private static RecipeProduct Input(int id, int recipeId, int productId, double quantity)
	{
		return new RecipeProduct
		{
			Recipe_Product_Id = id,
			Recipe_Id = recipeId,
			Product_Id = productId,
			Quantity = quantity,
			Is_Input = true
		};
	}

	private static RecipeProduct Output(int id, int recipeId, int productId, double quantity)
	{
		return new RecipeProduct
		{
			Recipe_Product_Id = id,
			Recipe_Id = recipeId,
			Product_Id = productId,
			Quantity = quantity,
			Is_Input = false
		};
	}

	private static Machine Machine(int id, string name, double baseSpeed = 10.0)
	{
		return new Machine
		{
			Machine_Id = id,
			Project_Id = 1,
			Puid = $"machine{id}",
			Name = name,
			Base_Speed = baseSpeed,
			Version = 1,
			Created_At = StaticNow,
			Last_Updated = StaticNow
		};
	}

	private static WorkflowSolver CreateSolver()
	{
		return new WorkflowSolver(new MachineCalculator());
	}

	private static ProjectObjects BuildProjectObjects(
		List<Product> products,
		List<Recipe> recipes,
		List<RecipeProduct> recipeProducts,
		List<Modifier>? modifiers = null,
		List<Machine>? machines = null)
	{
		return new ProjectObjects
		{
			Products = products,
			Attributes = [],
			Recipes = recipes,
			RecipeProducts = recipeProducts,
			RecipeAttributes = [],
			Machines = machines ?? [Machine(1, "Default")],
			MachineRecipes = [],
			MachineAttributes = [],
			Modifiers = modifiers ?? [],
			ModifierAttributes = []
		};
	}

	private static Modifier Modifier(int id, string name, double inputMult, double outputMult)
	{
		return new Modifier
		{
			Modifier_Id = id,
			Project_Id = 1,
			Puid = $"m{id}",
			Name = name,
			Flat_Bonus = 0,
			Percent_Bonus = 0,
			Multiplicative_Bonus = 1,
			Input_Percent = inputMult,
			Output_Percent = outputMult,
			Version = 1,
			Created_At = StaticNow,
			Last_Updated = StaticNow
		};
	}

	private static FullWorkflowModifier WorkflowNodeModifier(int id, int nodeId, int modifierId)
	{
		return new FullWorkflowModifier
		{
			Modifier = new WorkflowNodeModifier
			{
				Workflow_Node_Modifier_Id = id,
				Workflow_Node_Id = nodeId,
				Modifier_Id = modifierId,
				Modifier_Version = 1
			},
			ModifierAttributes = []
		};
	}

	private static NodeChart BuildDemandNodeChart(
		IEnumerable<(int productId, double targetRate)> targets,
		IEnumerable<int>? externalProductIds = null,
		IEnumerable<int>? preferredRecipeIds = null,
		IEnumerable<FullNode>? nodes = null)
	{
		return new NodeChart
		{
			Nodes = nodes?.ToList() ?? [],
			Edges = [],
			Targets = targets.Select((t, i) => new WorkflowTarget
			{
				Workflow_Target_Id = i + 1,
				Workflow_Id = 1,
				Product_Id = t.productId,
				Target_Rate = t.targetRate
			}).ToList(),
			ProductNodes = (externalProductIds ?? []).Select((id, i) => new WorkflowProductNode
			{
				Workflow_Product_Node_Id = i + 1,
				Workflow_Id = 1,
				Product_Id = id,
				Calculated_Flow_Rate = 0.0,
				Actual_Flow_Rate_In = 0.0,
				Actual_Flow_Rate_Out = 0.0,
				Is_External = true
			}).ToList(),
			PreferredRecipes = (preferredRecipeIds ?? []).Select((id, i) => new WorkflowRecipe
			{
				Workflow_Recipe_Id = i + 1,
				Workflow_Id = 1,
				Recipe_Id = id
			}).ToList()
		};
	}

	private static FullNode BuildNode(int recipeId, double? actualMachineCount, double? calculatedMachineCount, double? calculatedTargetRate, int? machineId = 1)
	{
		return new FullNode
		{
			Node = new WorkflowNode
			{
				Node_Id = recipeId,
				Workflow_Id = 1,
				Puid = $"node-{recipeId}",
				Recipe_Id = recipeId,
				Recipe_Version = 1,
				Machine_Id = machineId,
				Actual_Machine_Count = actualMachineCount,
				Calculated_Machine_Count = calculatedMachineCount,
				Calculated_Target_Rate = calculatedTargetRate
			},
			Modifiers = []
		};
	}

	private static NodeChart BuildSupplyNodeChart(
		IEnumerable<FullNode> nodes,
		IEnumerable<(int productId, double targetRate)>? targets = null,
		IEnumerable<(int productId, double importRate)>? externalImports = null)
	{
		return new NodeChart
		{
			Nodes = nodes.ToList(),
			Edges = [],
			Targets = (targets ?? []).Select((t, i) => new WorkflowTarget
			{
				Workflow_Target_Id = i + 1,
				Workflow_Id = 1,
				Product_Id = t.productId,
				Target_Rate = t.targetRate
			}).ToList(),
			ProductNodes = (externalImports ?? []).Select((e, i) => new WorkflowProductNode
			{
				Workflow_Product_Node_Id = i + 1,
				Workflow_Id = 1,
				Product_Id = e.productId,
				Calculated_Flow_Rate = 0.0,
				Actual_Flow_Rate_In = e.importRate,
				Actual_Flow_Rate_Out = 0.0,
				Is_External = true
			}).ToList(),
			PreferredRecipes = []
		};
	}

	private static void AssertRate(Dictionary<int, double> rates, int recipeId, double expected, double tolerance = 1e-6)
    {
        Assert.True(rates.TryGetValue(recipeId, out var actual), $"Expected recipe {recipeId} in solution.");
        Assert.InRange(actual, expected - tolerance, expected + tolerance);
    }

    private static void AssertFlow(IReadOnlyDictionary<int, double> flow, int productId, double expected, double tolerance = 1e-6)
    {
        if (expected <= 0)
        {
            Assert.False(flow.TryGetValue(productId, out _), $"Expected no flow for product {productId}.");
        }
        else
        {
            Assert.True(flow.TryGetValue(productId, out var actual), $"Expected flow for product {productId} in solution.");
            Assert.InRange(actual, expected - tolerance, expected + tolerance);
        }
    }

	[Fact]
	public void SolveDemand_LinearWorkflow_ReturnsCorrect()
	{
		var raw = Product(1, "Raw");
		var final = Product(2, "Final");
		var recipe = Recipe(10, "MakeFinal");

		var projectObjects = BuildProjectObjects(
			[raw, final],
			[recipe],
			[Input(1, 10, 1, 1.0), Output(2, 10, 2, 1.0)]);

		var nodeChart = BuildDemandNodeChart([(2, 10.0)], externalProductIds: [1]);

		var result = CreateSolver().SolveDemand(projectObjects, nodeChart);

		Assert.Single(result);
		AssertRate(result, 10, 10.0);
	}

	[Fact]
	public void SolveDemand_HandlesMultipleTargets_ReturnsCorrect()
	{
		var rawA = Product(1, "RawA");
		var rawC = Product(3, "RawC");
		var outB = Product(2, "B");
		var outD = Product(4, "D");
		var recipeB = Recipe(10, "MakeB");
		var recipeD = Recipe(20, "MakeD");

		var projectObjects = BuildProjectObjects(
			[rawA, outB, rawC, outD],
			[recipeB, recipeD],
			[
				Input(1, 10, 1, 1.0), Output(2, 10, 2, 1.0),
				Input(3, 20, 3, 1.0), Output(4, 20, 4, 1.0)
			]);

		var nodeChart = BuildDemandNodeChart([(2, 5.0), (4, 7.0)], externalProductIds: [1, 3]);

		var result = CreateSolver().SolveDemand(projectObjects, nodeChart);

		Assert.Equal(2, result.Count);
		AssertRate(result, 10, 5.0);
		AssertRate(result, 20, 7.0);
	}

	[Fact]
	public void SolveDemand_HandlesIntermediateProducts_ReturnsCorrect()
	{
		var raw = Product(1, "Raw");
		var intermediate = Product(2, "Intermediate");
		var final = Product(3, "Final");
		var makeIntermediate = Recipe(10, "MakeIntermediate");
		var makeFinal = Recipe(20, "MakeFinal");

		var projectObjects = BuildProjectObjects(
			[raw, intermediate, final],
			[makeIntermediate, makeFinal],
			[
				Input(1, 10, 1, 1.0), Output(2, 10, 2, 1.0),
				Input(3, 20, 2, 2.0), Output(4, 20, 3, 1.0)
			]);

		var nodeChart = BuildDemandNodeChart([(3, 4.0)], externalProductIds: [1]);

		var result = CreateSolver().SolveDemand(projectObjects, nodeChart);

		Assert.Equal(2, result.Count);
		AssertRate(result, 20, 4.0);
		AssertRate(result, 10, 8.0);
	}

	[Theory]
	[InlineData(8.0, 0.0, 2.0)]
	[InlineData(8.0, 2.0, 2.0)]
	[InlineData(8.0, 5.0, 5.0)]
	[InlineData(0.0, 3.0, 3.0)]
	public void SolveDemand_MultiInputMultiOutputRecipe_ReturnsCorrect(double targetC, double targetD, double expectedRate)
	{
		var a = Product(1, "A");
		var b = Product(2, "B");
		var c = Product(3, "C");
		var d = Product(4, "D");
		var recipe = Recipe(10, "JointRecipe");

		var projectObjects = BuildProjectObjects(
			[a, b, c, d],
			[recipe],
			[
				Input(1, 10, 1, 2.0), Input(2, 10, 2, 3.0),
				Output(3, 10, 3, 4.0), Output(4, 10, 4, 1.0)
			]);

		var targets = new List<(int productId, double targetRate)>();
		if (targetC > 0.0) targets.Add((3, targetC));
		if (targetD > 0.0) targets.Add((4, targetD));

		var nodeChart = BuildDemandNodeChart(targets, externalProductIds: [1, 2]);

		var result = CreateSolver().SolveDemand(projectObjects, nodeChart);

		Assert.Single(result);
		AssertRate(result, 10, expectedRate);
	}

	[Fact]
	public void SolveDemand_CyclicalWorkflow_ReturnsCorrect()
	{
		var a = Product(1, "A");
		var b = Product(2, "B");
		var makeB = Recipe(10, "AtoB");
		var makeA = Recipe(20, "BtoA");

		var projectObjects = BuildProjectObjects(
			[a, b],
			[makeB, makeA],
			[
				Input(1, 10, 1, 1.0), Output(2, 10, 2, 1.0),
				Input(3, 20, 2, 1.0), Output(4, 20, 1, 1.0)
			]);

		var nodeChart = BuildDemandNodeChart([(2, 10.0)], externalProductIds: [1]);

		var result = CreateSolver().SolveDemand(projectObjects, nodeChart);

		AssertRate(result, 10, 10.0);
		Assert.False(result.ContainsKey(20));
	}

	[Fact]
	public void SolveDemand_ExternalImportAlwaysUsed_ReturnsCorrectUsingExternal()
	{
		var externalOnly = Product(1, "ExternalOnly");
		var projectObjects = BuildProjectObjects([externalOnly], [], []);
		var nodeChart = BuildDemandNodeChart([(1, 6.0)], externalProductIds: [1]);

		var result = CreateSolver().SolveDemand(projectObjects, nodeChart);

		Assert.Empty(result);
	}

	[Fact]
	public void SolveDemand_IncompleteWorkflowWithExternalProducts_ReturnsCorrect()
	{
		var imported = Product(1, "Imported");
		var final = Product(2, "Final");
		var recipe = Recipe(10, "ImportedToFinal");

		var projectObjects = BuildProjectObjects(
			[imported, final],
			[recipe],
			[Input(1, 10, 1, 1.0), Output(2, 10, 2, 1.0)]);

		var nodeChart = BuildDemandNodeChart([(2, 5.0)], externalProductIds: [1]);

		var result = CreateSolver().SolveDemand(projectObjects, nodeChart);

		Assert.Single(result);
		AssertRate(result, 10, 5.0);
	}

	[Fact]
	public void SolveDemand_PreferredRecipeHasLowerCost_ReturnsCorrectWithPreferred()
	{
		var raw = Product(1, "Raw");
		var final = Product(2, "Final");
		var nonPreferred = Recipe(10, "NonPreferred");
		var preferred = Recipe(20, "Preferred");

		var projectObjects = BuildProjectObjects(
			[raw, final],
			[nonPreferred, preferred],
			[
				Input(1, 10, 1, 1.0), Output(2, 10, 2, 1.0),
				Input(3, 20, 1, 1.0), Output(4, 20, 2, 1.0)
			]);

		var nodeChart = BuildDemandNodeChart([(2, 10.0)], externalProductIds: [1], preferredRecipeIds: [20]);

		var result = CreateSolver().SolveDemand(projectObjects, nodeChart);

		AssertRate(result, 20, 10.0);
		Assert.False(result.ContainsKey(10));
	}

	[Fact]
	public void SolveDemand_TargetProductWithMultiplePaths_SelectsOptimalRecipe()
	{
		var raw = Product(1, "Raw");
		var intermediate = Product(2, "Intermediate");
		var final = Product(3, "Final");
		var direct = Recipe(10, "Direct");
		var step1 = Recipe(20, "Step1");
		var step2 = Recipe(30, "Step2");

		var projectObjects = BuildProjectObjects(
			[raw, intermediate, final],
			[direct, step1, step2],
			[
				Input(1, 10, 1, 1.0), Output(2, 10, 3, 1.0),
				Input(3, 20, 1, 1.0), Output(4, 20, 2, 1.0),
				Input(5, 30, 2, 1.0), Output(6, 30, 3, 1.0)
			]);

		var nodeChart = BuildDemandNodeChart([(3, 10.0)], externalProductIds: [1]);

		var result = CreateSolver().SolveDemand(projectObjects, nodeChart);

		AssertRate(result, 10, 10.0);
		Assert.False(result.ContainsKey(20));
		Assert.False(result.ContainsKey(30));
	}

	[Fact]
	public void SolveDemand_ZeroTarget_ReturnsEmptyRates()
	{
		var raw = Product(1, "Raw");
		var final = Product(2, "Final");
		var recipe = Recipe(10, "MakeFinal");

		var projectObjects = BuildProjectObjects(
			[raw, final],
			[recipe],
			[Input(1, 10, 1, 1.0), Output(2, 10, 2, 1.0)]);

		var nodeChart = BuildDemandNodeChart([(2, 0.0)], externalProductIds: [1]);

		var result = CreateSolver().SolveDemand(projectObjects, nodeChart);

		Assert.Empty(result);
	}

	[Fact]
	public void SolveDemand_NegativeTarget_ThrowsException()
	{
		var final = Product(1, "Final");
		var projectObjects = BuildProjectObjects([final], [], []);
		var nodeChart = BuildDemandNodeChart([(1, -1.0)]);

		Assert.Throws<ArgumentOutOfRangeException>(() => CreateSolver().SolveDemand(projectObjects, nodeChart));
	}

	[Fact]
	public void SolveDemand_IncompleteWorkflow_ThrowsException()
	{
		var target = Product(1, "Target");
		var projectObjects = BuildProjectObjects([target], [], []);
		var nodeChart = BuildDemandNodeChart([(1, 1.0)]);

		Assert.Throws<InvalidOperationException>(() => CreateSolver().SolveDemand(projectObjects, nodeChart));
	}

	[Fact]
	public void SolveDemand_RecipeWithNoOutputs_IgnoredInSolution()
	{
		var raw = Product(1, "Raw");
		var final = Product(2, "Final");
		var noOutput = Recipe(10, "NoOutput");
		var valid = Recipe(20, "Valid");

		var projectObjects = BuildProjectObjects(
			[raw, final],
			[noOutput, valid],
			[
				Input(1, 10, 1, 1.0),
				Input(2, 20, 1, 1.0), Output(3, 20, 2, 1.0)
			]);

		var nodeChart = BuildDemandNodeChart([(2, 4.0)], externalProductIds: [1]);

		var result = CreateSolver().SolveDemand(projectObjects, nodeChart);

		AssertRate(result, 20, 4.0);
		Assert.False(result.ContainsKey(10));
	}

	[Fact]
	public void SolveDemand_HandlesFloatingPointPrecision_CorrectRates()
	{
		var raw = Product(1, "Raw");
		var final = Product(2, "Final");
		var recipe = Recipe(10, "Fractional");

		var projectObjects = BuildProjectObjects(
			[raw, final],
			[recipe],
			[Input(1, 10, 1, 0.2), Output(2, 10, 2, 0.1)]);

		var nodeChart = BuildDemandNodeChart([(2, 0.3)], externalProductIds: [1]);

		var result = CreateSolver().SolveDemand(projectObjects, nodeChart);

		AssertRate(result, 10, 3.0, 1e-5);
	}

	[Fact]
	public void SolveSupply_CompleteWorkflow_ReturnsMaxRates()
	{
		var raw = Product(1, "Raw");
		var final = Product(2, "Final");
		var recipe = Recipe(10, "MakeFinal");

		var projectObjects = BuildProjectObjects(
			[raw, final],
			[recipe],
			[Input(1, 10, 1, 1.0), Output(2, 10, 2, 1.0)]);

		var nodeChart = BuildSupplyNodeChart(
			nodes: [BuildNode(10, 1.0, 1.0, 10.0)],
			targets: [(2, 10.0)],
			externalImports: [(1, 10.0)]);

		var result = CreateSolver().SolveSupply(projectObjects, nodeChart);

        Assert.Single(result.RecipeRates);
        AssertRate(result.RecipeRates.ToDictionary(), 10, 10.0);
        AssertFlow(result.ProductInFlowRates, 1, 10.0);
        AssertFlow(result.ProductOutFlowRates, 2, 10.0);
    }

	[Fact]
	public void SolveSupply_IncompleteWorkflow_ReturnsZeroForUnproducibleProducts()
	{
		var raw = Product(1, "Raw");
		var final = Product(2, "Final");
		var recipe = Recipe(10, "MakeFinal");

		var projectObjects = BuildProjectObjects(
			[raw, final],
			[recipe],
			[Input(1, 10, 1, 1.0), Output(2, 10, 2, 1.0)]);

		var nodeChart = BuildSupplyNodeChart(
			nodes: [BuildNode(10, 1.0, 1.0, 10.0)],
			targets: [(2, 10.0)],
			externalImports: []);

		var result = CreateSolver().SolveSupply(projectObjects, nodeChart);

        Assert.Empty(result.RecipeRates);
        Assert.Empty(result.ProductOutFlowRates);
    }

	[Fact]
	public void SolveSupply_OverbuiltWorkflow_RespectsOverflowRates()
	{
		var raw = Product(1, "Raw");
		var final = Product(2, "Final");
		var recipe = Recipe(10, "MakeFinal");

		var projectObjects = BuildProjectObjects(
			[raw, final],
			[recipe],
			[Input(1, 10, 1, 1.0), Output(2, 10, 2, 1.0)]);

		var nodeChart = BuildSupplyNodeChart(
			nodes: [BuildNode(10, 2.0, 1.0, 10.0)],
			targets: [(2, 5.0)],
			externalImports: [(1, 20.0)]);

		var result = CreateSolver().SolveSupply(projectObjects, nodeChart);

		Assert.Single(result.RecipeRates);
        AssertRate(result.RecipeRates.ToDictionary(), 10, 20.0);
        AssertFlow(result.ProductInFlowRates, 1, 20.0);
        AssertFlow(result.ProductOutFlowRates, 2, 20.0);
    }

	[Fact]
	public void SolveSupply_ExternalImport_RespectsActualFlowRate()
	{
		var raw = Product(1, "Raw");
		var final = Product(2, "Final");
		var recipe = Recipe(10, "MakeFinal");

		var projectObjects = BuildProjectObjects(
			[raw, final],
			[recipe],
			[Input(1, 10, 1, 1.0), Output(2, 10, 2, 1.0)]);

		var nodeChart = BuildSupplyNodeChart(
			nodes: [BuildNode(10, 2.0, 1.0, 10.0)],
			targets: [(2, 20.0)],
			externalImports: [(1, 7.0)]);

		var result = CreateSolver().SolveSupply(projectObjects, nodeChart);

		Assert.Single(result.RecipeRates);
        AssertRate(result.RecipeRates.ToDictionary(), 10, 7.0);
        AssertFlow(result.ProductInFlowRates, 1, 7.0);
    }

	[Fact]
	public void SolveSupply_HandlesMultipleNodes_ReturnsMaxRates()
	{
		var rawA = Product(1, "RawA");
		var outB = Product(2, "B");
		var rawC = Product(3, "RawC");
		var outD = Product(4, "D");
		var makeB = Recipe(10, "MakeB");
		var makeD = Recipe(20, "MakeD");

		var projectObjects = BuildProjectObjects(
			[rawA, outB, rawC, outD],
			[makeB, makeD],
			[
				Input(1, 10, 1, 1.0), Output(2, 10, 2, 1.0),
				Input(3, 20, 3, 1.0), Output(4, 20, 4, 1.0)
			]);

		var nodeChart = BuildSupplyNodeChart(
			nodes:
			[
				BuildNode(10, 1.0, 1.0, 5.0),
				BuildNode(20, 1.0, 1.0, 8.0)
			],
			targets: [(2, 5.0), (4, 8.0)],
			externalImports: [(1, 5.0), (3, 8.0)]);

		var result = CreateSolver().SolveSupply(projectObjects, nodeChart);

		Assert.Equal(2, result.RecipeRates.Count);
        AssertRate(result.RecipeRates.ToDictionary(), 10, 5.0);
        AssertRate(result.RecipeRates.ToDictionary(), 20, 8.0);
        AssertFlow(result.ProductOutFlowRates, 2, 5.0);
        AssertFlow(result.ProductOutFlowRates, 4, 8.0);
    }

	[Fact]
	public void SolveSupply_HandlesZeroMachineCount_ReturnsZeroRates()
	{
		var raw = Product(1, "Raw");
		var final = Product(2, "Final");
		var recipe = Recipe(10, "MakeFinal");

		var projectObjects = BuildProjectObjects(
			[raw, final],
			[recipe],
			[Input(1, 10, 1, 1.0), Output(2, 10, 2, 1.0)]);

		var nodeChart = BuildSupplyNodeChart(
			nodes: [BuildNode(10, 0.0, 1.0, 10.0)],
			targets: [(2, 10.0)],
			externalImports: [(1, 10.0)]);

		var result = CreateSolver().SolveSupply(projectObjects, nodeChart);

		Assert.Empty(result.RecipeRates);
    }

	[Fact]
	public void SolveSupply_HandlesNegativeMachineCount_ThrowsException()
	{
		var raw = Product(1, "Raw");
		var final = Product(2, "Final");
		var recipe = Recipe(10, "MakeFinal");

		var projectObjects = BuildProjectObjects(
			[raw, final],
			[recipe],
			[Input(1, 10, 1, 1.0), Output(2, 10, 2, 1.0)]);

		var nodeChart = BuildSupplyNodeChart(
			nodes: [BuildNode(10, -1.0, 1.0, 10.0)],
			targets: [(2, 10.0)],
			externalImports: [(1, 10.0)]);

		Assert.Throws<ArgumentOutOfRangeException>(() => CreateSolver().SolveSupply(projectObjects, nodeChart));
	}

	[Fact]
	public void SolveSupply_HandlesOverflowSink_ReturnsMaxRates()
	{
		var raw = Product(1, "Raw");
		var final = Product(2, "Final");
		var recipe = Recipe(10, "MakeFinal");

		var projectObjects = BuildProjectObjects(
			[raw, final],
			[recipe],
			[Input(1, 10, 1, 1.0), Output(2, 10, 2, 1.0)]);

		var nodeChart = BuildSupplyNodeChart(
			nodes: [BuildNode(10, 2.0, 1.0, 10.0)],
			targets: [(2, 5.0)],
			externalImports: [(1, 20.0)]);

		var result = CreateSolver().SolveSupply(projectObjects, nodeChart);

		Assert.Single(result.RecipeRates);
        AssertRate(result.RecipeRates.ToDictionary(), 10, 20.0);
        AssertFlow(result.ProductOutFlowRates, 2, 20.0);
    }

	[Fact]
	public void SolveSupply_HandlesCyclicalWorkflow_CorrectRates()
	{
		var a = Product(1, "A");
		var b = Product(2, "B");
		var makeB = Recipe(10, "AtoB");
		var makeA = Recipe(20, "BtoA");

		var projectObjects = BuildProjectObjects(
			[a, b],
			[makeB, makeA],
			[
				Input(1, 10, 1, 1.0), Output(2, 10, 2, 1.0),
				Input(3, 20, 2, 1.0), Output(4, 20, 1, 1.0)
			]);

		var nodeChart = BuildSupplyNodeChart(
			nodes:
			[
				BuildNode(10, 1.0, 1.0, 10.0),
				BuildNode(20, 1.0, 1.0, 10.0)
			],
			targets: [(2, 10.0)],
			externalImports: [(1, 10.0)]);

		var result = CreateSolver().SolveSupply(projectObjects, nodeChart);

		AssertRate(result.RecipeRates.ToDictionary(), 10, 10.0);
        Assert.False(result.RecipeRates.TryGetValue(20, out var cycleRate) && cycleRate > 1e-5);
    }

	[Fact]
	public void SolveSupply_HandlesMultiInputMultiOutput_CorrectRates()
	{
		var a = Product(1, "A");
		var b = Product(2, "B");
		var c = Product(3, "C");
		var d = Product(4, "D");
		var recipe = Recipe(10, "JointRecipe");

		var projectObjects = BuildProjectObjects(
			[a, b, c, d],
			[recipe],
			[
				Input(1, 10, 1, 2.0), Input(2, 10, 2, 3.0),
				Output(3, 10, 3, 4.0), Output(4, 10, 4, 1.0)
			]);

		var nodeChart = BuildSupplyNodeChart(
			nodes: [BuildNode(10, 1.0, 1.0, 10.0)],
			targets: [(3, 20.0), (4, 5.0)],
			externalImports: [(1, 20.0), (2, 15.0)]);

		var result = CreateSolver().SolveSupply(projectObjects, nodeChart);

		Assert.Single(result.RecipeRates);
        AssertRate(result.RecipeRates.ToDictionary(), 10, 5.0);
        AssertFlow(result.ProductOutFlowRates, 3, 20.0);
        AssertFlow(result.ProductOutFlowRates, 4, 5.0);
    }

	[Fact]
	public void SolveDemand_WithOutputModifier_ReturnsCorrect()
	{
		var raw = Product(1, "Raw");
		var final = Product(2, "Final");
		var recipe = Recipe(10, "MakeFinal");
		var modifier = Modifier(100, "OutputBoost", 0.0, 0.5); // +50% output

		var projectObjects = BuildProjectObjects(
			[raw, final],
			[recipe],
			[Input(1, 10, 1, 1.0), Output(2, 10, 2, 1.0)],
			[modifier]);

		var node = BuildNode(10, null, null, null);
		node.Modifiers.Add(WorkflowNodeModifier(1, 10, 100));

		var nodeChart = BuildDemandNodeChart([(2, 15.0)], externalProductIds: [1], nodes: [node]);

		var result = CreateSolver().SolveDemand(projectObjects, nodeChart);

		// 1.0 * (1 + 0.5) = 1.5 per run. Need 15.0 units. Rate = 15.0 / 1.5 = 10.0.
		AssertRate(result, 10, 10.0);
	}

	[Fact]
	public void SolveDemand_WithInputModifier_ReturnsCorrect()
	{
		var raw = Product(1, "Raw");
		var intermediate = Product(2, "Intermediate");
		var final = Product(3, "Final");
		var recipe1 = Recipe(10, "RawToIntermediate");
		var recipe2 = Recipe(20, "IntermediateToFinal");
		var modifier = Modifier(100, "InputEfficiency", -0.5, 0.0); // 50% less input needed

		var projectObjects = BuildProjectObjects(
			[raw, intermediate, final],
			[recipe1, recipe2],
			[
				Input(1, 10, 1, 1.0), Output(2, 10, 2, 1.0),
				Input(3, 20, 2, 1.0), Output(4, 20, 3, 1.0)
			],
			[modifier]);

		var node2 = BuildNode(20, null, null, null);
		node2.Modifiers.Add(WorkflowNodeModifier(1, 20, 100));

		// Targeting 10 Final units. Recipe2 produces 1.0 Final per run. Rate2 = 10.
		// Recipe2 needs 1.0 * (1 - 0.5) = 0.5 Intermediate per run.
		// Total Intermediate needed = 10 * 0.5 = 5.0.
		// Recipe1 produces 1.0 Intermediate per run. Rate1 = 5.0.

		var nodeChart = BuildDemandNodeChart([(3, 10.0)], externalProductIds: [1], nodes: [node2]);

		var result = CreateSolver().SolveDemand(projectObjects, nodeChart);

		AssertRate(result, 20, 10.0);
		AssertRate(result, 10, 5.0);
	}

	[Fact]
	public void SolveSupply_WithOutputModifier_ReturnsCorrect()
	{
		var raw = Product(1, "Raw");
		var final = Product(2, "Final");
		var recipe = Recipe(10, "MakeFinal");
		var modifier = Modifier(100, "DoubleOutput", 0, 1.0); // +100% output

		var projectObjects = BuildProjectObjects(
			[raw, final],
			[recipe],
			[Input(1, 10, 1, 1.0), Output(2, 10, 2, 1.0)],
			[modifier]);

		var node = BuildNode(10, 10.0, 1.0, 1.0);
		node.Modifiers.Add(WorkflowNodeModifier(1, 10, 100));

		// 10 machines, each runs at rate 1.0. Total baseline rate = 10.0.
		// Output is 1.0 * (1 + 1.0) = 2.0 per run.
		// Total supply = 10.0 * 2.0 = 20.0.

		var nodeChart = BuildSupplyNodeChart(
			nodes: [node],
			targets: [(2, 25.0)],
			externalImports: [(1, 100.0)]);

		var result = CreateSolver().SolveSupply(projectObjects, nodeChart);

		AssertRate(result.RecipeRates.ToDictionary(), 10, 100.0);
        AssertFlow(result.ProductOutFlowRates, 2, 200.0);
    }

	[Fact]
	public void SolveSupply_WithMultipleModifiers_ReturnsCorrect()
	{
		var raw = Product(1, "Raw");
		var final = Product(2, "Final");
		var recipe = Recipe(10, "MakeFinal");
		var mod1 = Modifier(101, "Mod1", 0.0, 0.5); // +50% output
		var mod2 = Modifier(102, "Mod2", 0.0, 0.2); // +20% output

		var projectObjects = BuildProjectObjects(
			[raw, final],
			[recipe],
			[Input(1, 10, 1, 1.0), Output(2, 10, 2, 1.0)],
			[mod1, mod2]);

		var node = BuildNode(10, 10.0, 1.0, 1.0);
		node.Modifiers.Add(WorkflowNodeModifier(1, 10, 101));
		node.Modifiers.Add(WorkflowNodeModifier(2, 10, 102));

		// Combined output multiplier: (1 + 0.5) * (1 + 0.2) = 1 + 0.5 + 0.2 = 1.7
		// Rate 10.0 -> 17.0 units produced.

		var nodeChart = BuildSupplyNodeChart(
			nodes: [node],
			targets: [(2, 100.0)],
			externalImports: [(1, 100.0)]);

		var result = CreateSolver().SolveSupply(projectObjects, nodeChart);

		AssertRate(result.RecipeRates.ToDictionary(), 10, 100.0);
        AssertFlow(result.ProductOutFlowRates, 2, 170.0);
    }

	[Fact]
	public void SolveSupply_WithInputModifierBottleneck_ReturnsCorrect()
	{
		var raw = Product(1, "Raw");
		var final = Product(2, "Final");
		var recipe = Recipe(10, "MakeFinal");
		var modifier = Modifier(100, "InputEfficiency", -0.5, 0.0); // 50% less input needed

		var projectObjects = BuildProjectObjects(
			[raw, final],
			[recipe],
			[Input(1, 10, 1, 1.0), Output(2, 10, 2, 1.0)],
			[modifier]);

		var node = BuildNode(10, 20.0, 1.0, 1.0); // 20 units capacity
		node.Modifiers.Add(WorkflowNodeModifier(1, 10, 100));

		// Input: 1.0 * (1 - 0.5) = 0.5 Raw per run.
		// External supply of Raw: 5.0.
		// Maximum runs possible based on Raw: 5.0 / 0.5 = 10.0.
		// Machine capacity: 20.0.
		// Expected rate: 10.0.

		var nodeChart = BuildSupplyNodeChart(
			nodes: [node],
			targets: [(2, 100.0)],
			externalImports: [(1, 5.0)]);

		var result = CreateSolver().SolveSupply(projectObjects, nodeChart);

		AssertRate(result.RecipeRates.ToDictionary(), 10, 10.0);
        AssertFlow(result.ProductInFlowRates, 1, 5.0);
        AssertFlow(result.ProductOutFlowRates, 2, 10.0);
    }
}

