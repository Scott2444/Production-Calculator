using System.Diagnostics.CodeAnalysis;
using FakeItEasy;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Services;

namespace ProductionCalculator.Business.Tests.Services;

[ExcludeFromCodeCoverage]
public class ProjectDataServiceTests
{
    [Fact]
    public async Task GetProjectObjects_CallsAllRepositoriesAndAggregatesResult()
    {
        // Arrange
        var projectId = 1;
        var productRepo = A.Fake<IProductRepository>();
        var attributeRepo = A.Fake<IAttributeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var recipeProductRepo = A.Fake<IRecipeProductRepository>();
        var recipeAttributeRepo = A.Fake<IRecipeAttributeRepository>();
        var machineRepo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var machineAttributeRepo = A.Fake<IMachineAttributeRepository>();
        var modifierRepo = A.Fake<IModifierRepository>();
        var modifierAttributeRepo = A.Fake<IModifierAttributeRepository>();

        var recipes = new List<Recipe> 
        { 
            new Recipe { Recipe_Id = 10, Project_Id = projectId, Puid = "r1", Name = "R1", Base_Crafting_Time = 1, Version = 1, Created_At = DateTime.UtcNow, Last_Updated = DateTime.UtcNow } 
        };
        var machines = new List<Machine> 
        { 
            new Machine { Machine_Id = 20, Project_Id = projectId, Puid = "m1", Name = "M1", Base_Speed = 1, Version = 1, Created_At = DateTime.UtcNow, Last_Updated = DateTime.UtcNow } 
        };
        var modifiers = new List<Modifier> 
        { 
            new Modifier { Modifier_Id = 30, Project_Id = projectId, Puid = "mod1", Name = "Mod1", Flat_Bonus = 0, Percent_Bonus = 0, Multiplicative_Bonus = 1, Input_Percent = 0, Output_Percent = 0, Version = 1, Created_At = DateTime.UtcNow, Last_Updated = DateTime.UtcNow } 
        };

        A.CallTo(() => productRepo.GetProductsByProjectId(projectId)).Returns(new List<Product> { new Product { Product_Id = 1, Project_Id = projectId, Puid = "p1", Name = "P1", Created_At = DateTime.UtcNow, Last_Updated = DateTime.UtcNow } });
        A.CallTo(() => attributeRepo.GetAttributesByProjectId(projectId)).Returns(new List<ProjectAttribute> { new ProjectAttribute { Attribute_Id = 1, Project_Id = projectId, Puid = "a1", Name = "A1", Unit = "u", Version = 1, Created_At = DateTime.UtcNow, Last_Updated = DateTime.UtcNow } });
        A.CallTo(() => recipeRepo.GetByProjectId(projectId)).Returns(recipes);
        A.CallTo(() => machineRepo.GetMachinesByProjectId(projectId)).Returns(machines);
        A.CallTo(() => modifierRepo.GetModifiersByProjectId(projectId)).Returns(modifiers);

        A.CallTo(() => recipeProductRepo.GetByRecipeId(10)).Returns(new List<RecipeProduct> { new RecipeProduct { Recipe_Product_Id = 1, Recipe_Id = 10, Product_Id = 1, Quantity = 1, Is_Input = true } });
        A.CallTo(() => recipeAttributeRepo.GetByRecipeId(10)).Returns(new List<RecipeAttribute> { new RecipeAttribute { Recipe_Attribute_Id = 1, Recipe_Id = 10, Attribute_Id = 1, Rate = 1, Version = 1, Created_At = DateTime.UtcNow, Last_Updated = DateTime.UtcNow } });
        A.CallTo(() => machineRecipeRepo.GetByMachineId(20)).Returns(new List<MachineRecipe> { new MachineRecipe { Machine_Recipe_Id = 1, Machine_Id = 20, Recipe_Id = 10 } });
        A.CallTo(() => machineAttributeRepo.GetByMachineId(20)).Returns(new List<MachineAttribute> { new MachineAttribute { Machine_Attribute_Id = 1, Machine_Id = 20, Attribute_Id = 1, Rate = 1, Version = 1, Created_At = DateTime.UtcNow, Last_Updated = DateTime.UtcNow } });
        A.CallTo(() => modifierAttributeRepo.GetByModifierId(30)).Returns(new List<ModifierAttribute> { new ModifierAttribute { Modifier_Attribute_Id = 1, Modifier_Id = 30, Attribute_Id = 1, Flat_Bonus = 0, Percent_Bonus = 0, Multiplicative_Bonus = 1, Version = 1, Created_At = DateTime.UtcNow, Last_Updated = DateTime.UtcNow } });

        var service = new ProjectDataService(
            productRepo, attributeRepo, recipeRepo, recipeProductRepo, recipeAttributeRepo,
            machineRepo, machineRecipeRepo, machineAttributeRepo, modifierRepo, modifierAttributeRepo);

        // Act
        var result = await service.GetProjectObjects(projectId);

        // Assert
        Assert.Single(result.Products);
        Assert.Single(result.Attributes);
        Assert.Single(result.Recipes);
        Assert.Single(result.RecipeProducts);
        Assert.Single(result.RecipeAttributes);
        Assert.Single(result.Machines);
        Assert.Single(result.MachineRecipes);
        Assert.Single(result.MachineAttributes);
        Assert.Single(result.Modifiers);
        Assert.Single(result.ModifierAttributes);

        A.CallTo(() => productRepo.GetProductsByProjectId(projectId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => attributeRepo.GetAttributesByProjectId(projectId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => recipeRepo.GetByProjectId(projectId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => recipeProductRepo.GetByRecipeId(10)).MustHaveHappenedOnceExactly();
        A.CallTo(() => recipeAttributeRepo.GetByRecipeId(10)).MustHaveHappenedOnceExactly();
        A.CallTo(() => machineRepo.GetMachinesByProjectId(projectId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => machineRecipeRepo.GetByMachineId(20)).MustHaveHappenedOnceExactly();
        A.CallTo(() => machineAttributeRepo.GetByMachineId(20)).MustHaveHappenedOnceExactly();
        A.CallTo(() => modifierRepo.GetModifiersByProjectId(projectId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => modifierAttributeRepo.GetByModifierId(30)).MustHaveHappenedOnceExactly();
    }
}
