namespace ProductionCalculator.Business.APIModels
{
    public class WorkflowModifierExchange
    {
        public required string Puid { get; set; }
        public required List<WorkflowModifierAttributeExchange> Attributes { get; set; }
    }
}
