namespace ProductionCalculator.Business.Models
{
    public class ProjectObjects
    {
        public required List<Product> Products { get; set; } = [];
        public required List<Recipe> Recipes { get; set; } = [];
        public required List<RecipeProduct> RecipeProducts { get; set; } = [];
        public required List<Machine> Machines { get; set; } = [];
        public required List<MachineRecipe> MachineRecipes { get; set; } = [];
        public required List<Modifier> Modifiers { get; set; } = [];
    }
}