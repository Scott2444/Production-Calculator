using System.Diagnostics.CodeAnalysis;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Services;

namespace ProductionCalculator.Business.Tests.Services;

[ExcludeFromCodeCoverage]
public class MachineCalculatorTests
{
    private readonly MachineCalculator _calculator;

    public MachineCalculatorTests()
    {
        _calculator = new MachineCalculator();
    }

    [Fact]
    public void CalculateMachineCount_NoModifiers_ReturnsCorrectCount()
    {
        // rate 1/s, crafting time 1s, speed 1 -> 1 machine
        var recipe = new Recipe { Base_Crafting_Time = 1, Recipe_Id = 1, Project_Id = 1, Puid = "r", Name = "R", Version = 1, Created_At = DateTime.UtcNow, Last_Updated = DateTime.UtcNow };
        var machine = new Machine { Base_Speed = 1, Machine_Id = 1, Project_Id = 1, Puid = "m", Name = "M", Version = 1, Created_At = DateTime.UtcNow, Last_Updated = DateTime.UtcNow };
        var modifiers = new List<Modifier>();

        var result = _calculator.CalculateMachineCount(1.0, recipe, machine, modifiers);

        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CalculateMachineCount_WithModifiers_ReturnsCorrectCount()
    {
        // rate 2/s, crafting time 5s, speed 1
        // Modifiers: flat +1, percent +50%, multiplicative x2
        // Effective speed = (1 + 1) * (1 + 0.5) * 2 = 2 * 1.5 * 2 = 6
        // Recipes/s/machine = 6 / 5 = 1.2
        // Machine count = 2 / 1.2 = 1.666...
        var recipe = new Recipe { Base_Crafting_Time = 5, Recipe_Id = 1, Project_Id = 1, Puid = "r", Name = "R", Version = 1, Created_At = DateTime.UtcNow, Last_Updated = DateTime.UtcNow };
        var machine = new Machine { Base_Speed = 1, Machine_Id = 1, Project_Id = 1, Puid = "m", Name = "M", Version = 1, Created_At = DateTime.UtcNow, Last_Updated = DateTime.UtcNow };
        var modifiers = new List<Modifier>
        {
            new Modifier { Flat_Bonus = 1.0, Percent_Bonus = 0.5, Multiplicative_Bonus = 2.0, Modifier_Id = 1, Project_Id = 1, Puid = "mod1", Name = "Mod1", Input_Percent = 0, Output_Percent = 0, Version = 1, Created_At = DateTime.UtcNow, Last_Updated = DateTime.UtcNow }
        };

        var result = _calculator.CalculateMachineCount(2.0, recipe, machine, modifiers);

        Assert.Equal(2.0 / 1.2, result, 5);
    }

    [Fact]
    public void CalculateRecipeRate_WithModifiers_ReturnsCorrectRate()
    {
        // 10 machines, crafting time 2s, speed 0.5
        // Modifiers: flat +0.5, percent +100%, multiplicative x1.5
        // Effective speed = (0.5 + 0.5) * (1 + 1.0) * 1.5 = 1 * 2 * 1.5 = 3
        // Recipes/s/machine = 3 / 2 = 1.5
        // Recipe rate = 10 * 1.5 = 15
        var recipe = new Recipe { Base_Crafting_Time = 2, Recipe_Id = 1, Project_Id = 1, Puid = "r", Name = "R", Version = 1, Created_At = DateTime.UtcNow, Last_Updated = DateTime.UtcNow };
        var machine = new Machine { Base_Speed = 0.5, Machine_Id = 1, Project_Id = 1, Puid = "m", Name = "M", Version = 1, Created_At = DateTime.UtcNow, Last_Updated = DateTime.UtcNow };
        var modifiers = new List<Modifier>
        {
            new Modifier { Flat_Bonus = 0.5, Percent_Bonus = 1.0, Multiplicative_Bonus = 1.5, Modifier_Id = 1, Project_Id = 1, Puid = "mod1", Name = "Mod1", Input_Percent = 0, Output_Percent = 0, Version = 1, Created_At = DateTime.UtcNow, Last_Updated = DateTime.UtcNow }
        };

        var result = _calculator.CalculateRecipeRate(10.0, recipe, machine, modifiers);

        Assert.Equal(15.0, result);
    }
}
