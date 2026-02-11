using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IMachineCalculator
    {
        double CalculateMachineCount(double rate, Recipe recipe, Machine machine, List<Modifier> modifiers);
        double CalculateRecipeRate(double numMachines, Recipe recipe, Machine machine, List<Modifier> modifiers);
    }
}

