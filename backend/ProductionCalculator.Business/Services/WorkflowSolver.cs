using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using Google.OrTools.LinearSolver;

namespace ProductionCalculator.Business.Services
{
    public class WorkflowSolver : IWorkflowSolver
    {
        private const double EXTERNAL_IMPORT = 0.0001; // Very low cost for externally provided products
        private const double PREFERRED_RECIPE = 0.01;
        private const double DEFAULT_COST = 1.0; // Default cost for recipes
        private const double TARGET_BONUS = 100000.0; // Bonus to encourage meeting target supply
        private const double OVERFLOW_BONUS = 1000.0; // Bonus to encourage producing this product

        public WorkflowSolver() {}

        /// <summary>
        /// Uses a linear solver for demand calculation based on the provided project objects and node chart.
        /// Returns recipe rates as a dictionary mapping Recipe_Id to calculated rate.
        /// </summary>
        public Dictionary<int, double> SolveDemand(ProjectObjects projectObjects, NodeChart nodeChart)
        {
            if (nodeChart.Targets.Any(t => t.Target_Rate < 0.0))
            {
                throw new ArgumentOutOfRangeException(nameof(nodeChart), "Target rates must be non-negative.");
            }

            var recipeVarMap = new Dictionary<int, Variable>();
            var targetDict = nodeChart.Targets.ToDictionary(t => t.Product_Id, t => t.Target_Rate);
            var preferredRecipeIds = nodeChart.PreferredRecipes.Select(pr => pr.Recipe_Id).ToHashSet();

            var productsById = projectObjects.Products.ToDictionary(p => p.Product_Id);

            var externalProducts = nodeChart.ProductNodes
                .Where(pn => pn.Is_External)
                .Select(pn => productsById[pn.Product_Id])
                .ToList();

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
                // If this product is a requested target, set the floor to that rate.
                // Otherwise, it is 0 (Intermediate products must not be negative).
                targetDict.TryGetValue(product.Product_Id, out double minDemand);

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
            // Apply yield modifiers from the node chart to adjust the recipe-product quantities
            recipeProductNetQuantities = ScaleRecipeProductQuantities(recipeProductNetQuantities, projectObjects, nodeChart);
            
            // Fill coefficients by iterating the aggregated net quantities
            foreach (var kvp in recipeProductNetQuantities)
            {
                var recipeId = kvp.Key.Item1;
                var productId = kvp.Key.Item2;

                if (recipeVarMap.ContainsKey(recipeId) && productConstraintMap.ContainsKey(productId))
                {
                    Variable x = recipeVarMap[recipeId];
                    Constraint c = productConstraintMap[productId];

                    // Set the value from the matrix
                    c.SetCoefficient(x, kvp.Value);
                }
            }

            // Add import recipes (bounded by external flow rates)
            foreach (var externalProduct in externalProducts)
            {
                var productConstraint = productConstraintMap[externalProduct.Product_Id];
                var importRecipe = solver.MakeNumVar(0.0, double.PositiveInfinity, $"IMPORT_{externalProduct.Name}");
                
                productConstraint.SetCoefficient(importRecipe, 1.0);

                objective.SetCoefficient(importRecipe, EXTERNAL_IMPORT);
            }

            Solver.ResultStatus resultStatus = solver.Solve();

            if (resultStatus != Solver.ResultStatus.OPTIMAL)
            {
                throw new InvalidOperationException($"No optimal solution found for the demand calculation. Solver result status: {resultStatus}.");
            }

            // // TEMPORARY: FOR DEBUGGING PURPOSES ONLY
            // Console.WriteLine($"Optimization Successful! Total Cost: {objective.Value()}");
            // Console.WriteLine("------------------------------------------------");
            
            // // Join back to original Recipe list to get Names
            // foreach (var recipe in projectObjects.Recipes)
            // {
            //     var variable = recipeVarMap[recipe.Recipe_Id];
            //     if (variable.SolutionValue() > 1e-5)
            //     {
            //         Console.WriteLine($"Recipe '{recipe.Name}' (ID {recipe.Recipe_Id}): Run at rate {variable.SolutionValue():F2}");
            //     }
            // }

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
        public Dictionary<int, double> SolveSupply(ProjectObjects projectObjects, NodeChart nodeChart)
        {
            if (nodeChart.Targets.Any(t => t.Target_Rate < 0.0))
            {
                throw new ArgumentOutOfRangeException(nameof(nodeChart), "Target rates must be non-negative.");
            }

            if (nodeChart.Nodes.Any(n => (n.Node.Actual_Machine_Count ?? 0.0) < 0.0))
            {
                throw new ArgumentOutOfRangeException(nameof(nodeChart), "Actual machine counts must be non-negative.");
            }

            var recipeVarMap = new Dictionary<int, Variable>();
            var productConstraintMap = new Dictionary<int, Constraint>();
            var targetDict = nodeChart.Targets.ToDictionary(t => t.Product_Id, t => t.Target_Rate);

            var externalProducts = nodeChart.ProductNodes
                .Where(pn => pn.Is_External)
                .Select(pn => projectObjects.Products.First(p => p.Product_Id == pn.Product_Id))
                .ToList();

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

            Solver solver = Solver.CreateSolver("GLOP");
            Objective objective = solver.Objective();

            double GetMaxRecipeRate(FullNode fullNode)
            {
                var machineCount = fullNode.Node.Actual_Machine_Count
                    ?? fullNode.Node.Calculated_Machine_Count
                    ?? 0.0;
                
                var targetCount = fullNode.Node.Calculated_Machine_Count ?? 0.0;
                var targetRate = fullNode.Node.Calculated_Target_Rate ?? 0.0;

                if (machineCount <= 0.0 || targetCount <= 0.0 || targetRate <= 0.0)
                {
                    return 0.0;
                }

                return machineCount / targetCount * targetRate;
            }

            // Real recipe variables (bounded by machine capacity)
            foreach (var fullNode in nodeChart.Nodes)
            {
                var recipe = projectObjects.Recipes.First(r => r.Recipe_Id == fullNode.Node.Recipe_Id);
                var maxRate = GetMaxRecipeRate(fullNode);
                var variable = solver.MakeNumVar(0.0, maxRate, recipe.Name);
                recipeVarMap[recipe.Recipe_Id] = variable;
                objective.SetCoefficient(variable, 1.0);
            }

            // Create constraints for each product: Production - Consumption - Sinks >= 0
            foreach (var product in projectObjects.Products)
            {
                var constraint = solver.MakeConstraint(0.0, double.PositiveInfinity, product.Name);
                productConstraintMap[product.Product_Id] = constraint;
            }
            foreach (var externalProduct in externalProducts)
            {
                if (!productConstraintMap.ContainsKey(externalProduct.Product_Id))
                {
                    var constraint = solver.MakeConstraint(0.0, double.PositiveInfinity, externalProduct.Name);
                    productConstraintMap[externalProduct.Product_Id] = constraint;
                }
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
            // Apply yield modifiers from the node chart to adjust the recipe-product quantities
            recipeProductNetQuantities = ScaleRecipeProductQuantities(recipeProductNetQuantities, projectObjects, nodeChart);

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

            // Add import recipes (bounded by external flow rates)
            foreach (var externalProduct in externalProducts)
            {
                var productConstraint = productConstraintMap[externalProduct.Product_Id];
                var importRecipe = solver.MakeNumVar(0.0, Math.Max(0.0, externalRateByProductId[externalProduct.Product_Id]), $"IMPORT_{externalProduct.Name}");
                
                productConstraint.SetCoefficient(importRecipe, 1.0);

                objective.SetCoefficient(importRecipe, -EXTERNAL_IMPORT);
            }

            objective.SetMaximization();

            Solver.ResultStatus resultStatus = solver.Solve();

            if (resultStatus != Solver.ResultStatus.OPTIMAL)
            {
                throw new InvalidOperationException($"No optimal solution found for the supply calculation. Solver result status: {resultStatus}.");
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
                    // Console.WriteLine($"Recipe '{recipe.Name}' (ID {recipe.Recipe_Id}): Run at rate {value:F2}");
                }
            }

            return recipeRates;
        }

        private Dictionary<(int recipeId, int productId), double> 
            ScaleRecipeProductQuantities(
                Dictionary<(int recipeId, int productId), double> recipeProductNetQuantities,
                ProjectObjects projectObjects,
                NodeChart nodeChart)
        {
            var scaledRecipes = new Dictionary<int, (double inputMultipler, double outputPercent)>();
            foreach (var node in nodeChart.Nodes)
            {
                // Yield multipliers are additive
                var recipeId = node.Node.Recipe_Id;
                var inputPercent = 0.0;
                var outputPercent = 0.0;

                foreach (var workflowModifier in node.Modifiers)
                {
                    var modifier = projectObjects.Modifiers.First(m => m.Modifier_Id == workflowModifier.Modifier_Id);
                    inputPercent += modifier.Input_Percent;
                    outputPercent += modifier.Output_Percent;
                }

                scaledRecipes[recipeId] = (inputPercent, outputPercent);
            }
            foreach (var key in recipeProductNetQuantities.Keys.ToList())
            {
                var recipeId = key.recipeId;
                var productId = key.productId;
                if (scaledRecipes.TryGetValue(recipeId, out (double inputPercent, double outputPercent) multipliers))
                {
                    var originalQuantity = recipeProductNetQuantities[key];
                    var scaledQuantity = originalQuantity < 0
                        ? originalQuantity * (1 + multipliers.inputPercent)
                        : originalQuantity * (1 + multipliers.outputPercent);
                    recipeProductNetQuantities[key] = scaledQuantity;
                }
            }
            return recipeProductNetQuantities;
        }
    }
}