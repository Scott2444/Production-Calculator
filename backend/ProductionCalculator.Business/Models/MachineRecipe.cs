namespace ProductionCalculator.Business.Models
{
    public class MachineRecipe
    {
        public required int Machine_Recipe_Id { get; set; }
        public required int Recipe_Id { get; set; }
        public required int Machine_Id { get; set; }
    }
}
