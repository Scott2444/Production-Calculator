using System.Diagnostics.CodeAnalysis;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.API.Controllers;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.API.Tests.Controllers;

[ExcludeFromCodeCoverage]
public class RecipesControllerTests
{
    private static RecipesController CreateController(IRecipeService service)
    {
        var controller = new RecipesController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return controller;
    }

    private static RecipeResponse CreateRecipeResponse(string puid = "recipePuid", string name = "Recipe")
    {
        return new RecipeResponse
        {
            Puid = puid,
            Name = name,
            Description = "desc",
            BaseCraftingTime = 1.0,
            Inputs = new List<RecipeProductExchange>(),
            Outputs = new List<RecipeProductExchange>(),
            Attributes = new List<AttributeRateExchange>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetRecipeByPuid_ValidRequest_Returns200OkWithResponse()
    {
        var service = A.Fake<IRecipeService>();
        var response = CreateRecipeResponse();
        A.CallTo(() => service.GetRecipeByPuid("projPuid", "recipePuid")).Returns(ServiceResult<RecipeResponse>.SuccessResult(response));
        var controller = CreateController(service);

        var result = await controller.GetRecipeByPuid("projPuid", "recipePuid");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        var val = Assert.IsType<RecipeResponse>(obj.Value);
        Assert.Equal("recipePuid", val.Puid);
    }

    [Fact]
    public async Task GetRecipeByPuid_RecipeNotFound_Returns404NotFound()
    {
        var service = A.Fake<IRecipeService>();
        A.CallTo(() => service.GetRecipeByPuid("projPuid", "missing")).Returns(ServiceResult<RecipeResponse>.Fail(ServiceStatus.NotFound404, "Not Found"));
        var controller = CreateController(service);

        var result = await controller.GetRecipeByPuid("projPuid", "missing");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, obj.StatusCode);
    }

    [Fact]
    public async Task GetRecipesByProjectPuid_ValidRequest_Returns200OkWithList()
    {
        var service = A.Fake<IRecipeService>();
        var responseList = new List<RecipeResponse> { CreateRecipeResponse(puid: "r1"), CreateRecipeResponse(puid: "r2") };
        A.CallTo(() => service.GetRecipesByProjectPuid("projPuid")).Returns(ServiceResult<List<RecipeResponse>>.SuccessResult(responseList));
        var controller = CreateController(service);

        var result = await controller.GetRecipesByProjectPuid("projPuid");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        var val = Assert.IsType<List<RecipeResponse>>(obj.Value);
        Assert.Equal(2, val.Count);
    }

    [Fact]
    public async Task GetRecipesByProjectPuid_ProjectNotFound_Returns404NotFound()
    {
        var service = A.Fake<IRecipeService>();
        A.CallTo(() => service.GetRecipesByProjectPuid("missing")).Returns(ServiceResult<List<RecipeResponse>>.Fail(ServiceStatus.NotFound404, "Not Found"));
        var controller = CreateController(service);

        var result = await controller.GetRecipesByProjectPuid("missing");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, obj.StatusCode);
    }

    [Fact]
    public async Task AddRecipe_ValidRequest_Returns201Created()
    {
        var service = A.Fake<IRecipeService>();
        var response = CreateRecipeResponse();
        var req = new RecipeRequest { Name = "New", BaseCraftingTime = 1.0, Inputs = new(), Outputs = new() };
        A.CallTo(() => service.AddRecipe("projPuid", req.Name, req.Description, req.BaseCraftingTime, req.Inputs, req.Outputs, req.Attributes))
            .Returns(ServiceResult<RecipeResponse>.SuccessResult(response, ServiceStatus.Created201));
        var controller = CreateController(service);

        var result = await controller.AddRecipe("projPuid", req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, obj.StatusCode);
    }

    [Fact]
    public async Task AddRecipe_ServiceReturnsError_ReturnsErrorStatus()
    {
        var service = A.Fake<IRecipeService>();
        var req = new RecipeRequest { Name = "Existing", BaseCraftingTime = 1.0, Inputs = new(), Outputs = new() };
        A.CallTo(() => service.AddRecipe("projPuid", req.Name, req.Description, req.BaseCraftingTime, req.Inputs, req.Outputs, req.Attributes))
            .Returns(ServiceResult<RecipeResponse>.Fail(ServiceStatus.Conflict409, "Conflict"));
        var controller = CreateController(service);

        var result = await controller.AddRecipe("projPuid", req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(409, obj.StatusCode);
    }

    [Fact]
    public async Task UpdateRecipe_ValidRequest_Returns200OkWithResponse()
    {
        var service = A.Fake<IRecipeService>();
        var response = CreateRecipeResponse(puid: "r1", name: "Updated");
        var req = new RecipeRequest { Name = "Updated", BaseCraftingTime = 2.0, Inputs = new(), Outputs = new() };
        A.CallTo(() => service.UpdateRecipe("projPuid", "r1", req.Name, req.Description, req.BaseCraftingTime, req.Inputs, req.Outputs, req.Attributes))
            .Returns(ServiceResult<RecipeResponse>.SuccessResult(response));
        var controller = CreateController(service);

        var result = await controller.UpdateRecipe("projPuid", "r1", req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
    }

    [Fact]
    public async Task UpdateRecipe_ServiceReturnsError_ReturnsErrorStatus()
    {
        var service = A.Fake<IRecipeService>();
        var req = new RecipeRequest { Name = "Updated", BaseCraftingTime = 2.0, Inputs = new(), Outputs = new() };
        A.CallTo(() => service.UpdateRecipe("projPuid", "r1", req.Name, req.Description, req.BaseCraftingTime, req.Inputs, req.Outputs, req.Attributes))
            .Returns(ServiceResult<RecipeResponse>.Fail(ServiceStatus.BadRequest400, "Error"));
        var controller = CreateController(service);

        var result = await controller.UpdateRecipe("projPuid", "r1", req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, obj.StatusCode);
    }

    [Fact]
    public async Task DeleteRecipe_ValidRequest_Returns204NoContent()
    {
        var service = A.Fake<IRecipeService>();
        A.CallTo(() => service.DeleteRecipe("projPuid", "r1")).Returns(ServiceResult.SuccessResult(ServiceStatus.NoContent204));
        var controller = CreateController(service);

        var result = await controller.DeleteRecipe("projPuid", "r1");

        var obj = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(204, obj.StatusCode);
    }

    [Fact]
    public async Task DeleteRecipe_ServiceReturnsError_ReturnsErrorStatus()
    {
        var service = A.Fake<IRecipeService>();
        A.CallTo(() => service.DeleteRecipe("projPuid", "r1")).Returns(ServiceResult.Fail(ServiceStatus.NotFound404, "Not Found"));
        var controller = CreateController(service);

        var result = await controller.DeleteRecipe("projPuid", "r1");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, obj.StatusCode);
    }
}
