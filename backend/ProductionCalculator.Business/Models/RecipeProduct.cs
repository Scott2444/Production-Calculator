namespace ProductionCalculator.Business.Models
{
    public class RecipeProduct
    {
        public required int Recipe_Product_Id { get; set; }
        public required int Recipe_Id { get; set; }
        public required int Product_Id { get; set; }
        public required double Quantity { get; set; }
        public required bool Is_Input { get; set; }
    }
}
