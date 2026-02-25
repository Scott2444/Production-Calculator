namespace ProductionCalculator.Business.Records
{
    public record NodeUpdateImpact(bool RequiresDemandRecalculation, bool RequiresSupplyRecalculation);
}