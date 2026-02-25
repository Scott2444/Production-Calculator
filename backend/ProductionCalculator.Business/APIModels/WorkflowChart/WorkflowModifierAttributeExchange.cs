namespace ProductionCalculator.Business.APIModels
{
    public class WorkflowModifierAttributeExchange
    {
        public required string AttributePuid { get; set; }
        public required double FlatBonus { get; set; }
        public required double PercentBonus { get; set; }
        public required double MultiplicativeBonus { get; set; }
    }
}
