using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.APIModels
{
    public class RecipeProductExchange
    {
        public required string Puid { get; set; }
        public required double Quantity { get; set; }
    }
}
