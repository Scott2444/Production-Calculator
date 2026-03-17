namespace ProductionCalculator.Business.Records
{
    public record SolverSupplyResult(
        IReadOnlyDictionary<int, double> RecipeRates, 
        IReadOnlyDictionary<int, double> ProductInFlowRates,
        IReadOnlyDictionary<int, double> ProductOutFlowRates
    );
}