using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.API.Controllers;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.API.Tests;

public class ModifiersControllerTests
{
    private static ModifiersController CreateController(IModifierService service)
    {
        var controller = new ModifiersController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return controller;
    }

    private static Modifier CreateModifier(string puid = "modPuid", string name = "Modifier")
    {
        return new Modifier
        {
            Modifier_Id = 1,
            Project_Id = 1,
            Puid = puid,
            Name = name,
            Description = "desc",
            Flat_Bonus = 1.0,
            Percent_Bonus = 2.0,
            Multiplicative_Bonus = 3.0,
            Input_Multiplier = 1.0,
            Output_Multiplier = 1.0,
            Version = 1,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetModifierByPuid_ValidRequest_Returns200OkWithResponse()
    {
        var service = A.Fake<IModifierService>();
        var modifier = CreateModifier();
        A.CallTo(() => service.GetModifierByPuid("projPuid", "modPuid")).Returns(ServiceResult<Modifier>.SuccessResult(modifier));
        var controller = CreateController(service);

        var result = await controller.GetModifierByPuid("projPuid", "modPuid");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        var response = Assert.IsType<ModifierResponse>(obj.Value);
        Assert.Equal("modPuid", response.Puid);
    }

    [Fact]
    public async Task GetModifierByPuid_ModifierNotFound_Returns404NotFound()
    {
        var service = A.Fake<IModifierService>();
        A.CallTo(() => service.GetModifierByPuid("projPuid", "missing")).Returns(ServiceResult<Modifier>.Fail(ServiceStatus.NotFound404, "Not Found"));
        var controller = CreateController(service);

        var result = await controller.GetModifierByPuid("projPuid", "missing");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, obj.StatusCode);
    }

    [Fact]
    public async Task GetModifierByPuid_ServiceRedirect_Returns303SeeOther()
    {
        var service = A.Fake<IModifierService>();
        A.CallTo(() => service.GetModifierByPuid("alias", "modPuid"))
            .Returns(ServiceResult<Modifier>.Redirection(ServiceStatus.SeeOther303, "/api/projects/canonical/modifiers/modPuid"));
        var controller = CreateController(service);

        var result = await controller.GetModifierByPuid("alias", "modPuid");

        var obj = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(303, obj.StatusCode);
        Assert.Equal("/api/projects/canonical/modifiers/modPuid", controller.Response.Headers["Location"]);
    }

    [Fact]
    public async Task GetModifiersByProjectPuid_ValidRequest_Returns200OkWithList()
    {
        var service = A.Fake<IModifierService>();
        var modifiers = new List<Modifier> { CreateModifier(puid: "p1"), CreateModifier(puid: "p2") };
        A.CallTo(() => service.GetModifiersByProjectPuid("projPuid")).Returns(ServiceResult<List<Modifier>>.SuccessResult(modifiers));
        var controller = CreateController(service);

        var result = await controller.GetModifiersByProjectPuid("projPuid");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        var response = Assert.IsType<List<ModifierResponse>>(obj.Value);
        Assert.Equal(2, response.Count);
    }

    [Fact]
    public async Task GetModifiersByProjectPuid_ProjectNotFound_Returns404NotFound()
    {
        var service = A.Fake<IModifierService>();
        A.CallTo(() => service.GetModifiersByProjectPuid("missing")).Returns(ServiceResult<List<Modifier>>.Fail(ServiceStatus.NotFound404, "Not Found"));
        var controller = CreateController(service);

        var result = await controller.GetModifiersByProjectPuid("missing");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, obj.StatusCode);
    }

    [Fact]
    public async Task GetModifiersByProjectPuid_ServiceRedirect_Returns303SeeOther()
    {
        var service = A.Fake<IModifierService>();
        A.CallTo(() => service.GetModifiersByProjectPuid("alias"))
            .Returns(ServiceResult<List<Modifier>>.Redirection(ServiceStatus.SeeOther303, "/api/projects/canonical/modifiers"));
        var controller = CreateController(service);

        var result = await controller.GetModifiersByProjectPuid("alias");

        var obj = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(303, obj.StatusCode);
        Assert.Equal("/api/projects/canonical/modifiers", controller.Response.Headers["Location"]);
    }

    [Fact]
    public async Task AddModifier_ValidRequest_Returns201Created()
    {
        var service = A.Fake<IModifierService>();
        var modifier = CreateModifier();
        var req = new ModifierRequest { Name = "New", Description = "Desc", FlatBonus = 1, PercentBonus = 2, MultiplicativeBonus = 3, InputMultiplier = 1, OutputMultiplier = 1, Attributes = [] };
        A.CallTo(() => service.AddModifier("projPuid", req.Name, req.Description, req.FlatBonus, req.PercentBonus, req.MultiplicativeBonus, req.InputMultiplier, req.OutputMultiplier, req.Attributes))
            .Returns(ServiceResult<Modifier>.SuccessResult(modifier, ServiceStatus.Created201));
        var controller = CreateController(service);

        var result = await controller.AddModifier("projPuid", req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, obj.StatusCode);
        var response = Assert.IsType<ModifierResponse>(obj.Value);
        Assert.Equal(modifier.Puid, response.Puid);
    }

    [Fact]
    public async Task AddModifier_ServiceReturnsError_ReturnsErrorStatus()
    {
        var service = A.Fake<IModifierService>();
        var req = new ModifierRequest { Name = "Bad", Description = "Desc", FlatBonus = 1, PercentBonus = 2, MultiplicativeBonus = 3, InputMultiplier = 1, OutputMultiplier = 1, Attributes = [] };
        A.CallTo(() => service.AddModifier("projPuid", req.Name, req.Description, req.FlatBonus, req.PercentBonus, req.MultiplicativeBonus, req.InputMultiplier, req.OutputMultiplier, req.Attributes))
            .Returns(ServiceResult<Modifier>.Fail(ServiceStatus.BadRequest400, "Bad Request"));
        var controller = CreateController(service);

        var result = await controller.AddModifier("projPuid", req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, obj.StatusCode);
    }

    [Fact]
    public async Task UpdateModifier_ValidRequest_Returns200OkWithResponse()
    {
        var service = A.Fake<IModifierService>();
        var modifier = CreateModifier();
        var req = new ModifierRequest { Name = "Updated", Description = "Desc", FlatBonus = 1, PercentBonus = 2, MultiplicativeBonus = 3, InputMultiplier = 1, OutputMultiplier = 1, Attributes = [] };
        A.CallTo(() => service.UpdateModifier("projPuid", "modPuid", req.Name, req.Description, req.FlatBonus, req.PercentBonus, req.MultiplicativeBonus, req.InputMultiplier, req.OutputMultiplier, req.Attributes))
            .Returns(ServiceResult<Modifier>.SuccessResult(modifier, ServiceStatus.Ok200));
        var controller = CreateController(service);

        var result = await controller.UpdateModifier("projPuid", "modPuid", req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        var response = Assert.IsType<ModifierResponse>(obj.Value);
        Assert.Equal(modifier.Puid, response.Puid);
    }

    [Fact]
    public async Task UpdateModifier_ServiceReturnsError_ReturnsErrorStatus()
    {
        var service = A.Fake<IModifierService>();
        var req = new ModifierRequest { Name = "Bad", Description = "Desc", FlatBonus = 1, PercentBonus = 2, MultiplicativeBonus = 3, InputMultiplier = 1, OutputMultiplier = 1, Attributes = [] };
        A.CallTo(() => service.UpdateModifier("projPuid", "modPuid", req.Name, req.Description, req.FlatBonus, req.PercentBonus, req.MultiplicativeBonus, req.InputMultiplier, req.OutputMultiplier, req.Attributes))
            .Returns(ServiceResult<Modifier>.Fail(ServiceStatus.Conflict409, "Conflict"));
        var controller = CreateController(service);

        var result = await controller.UpdateModifier("projPuid", "modPuid", req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(409, obj.StatusCode);
    }

    [Fact]
    public async Task DeleteModifier_ValidRequest_Returns204NoContent()
    {
        var service = A.Fake<IModifierService>();
        A.CallTo(() => service.DeleteModifier("projPuid", "modPuid")).Returns(ServiceResult.SuccessResult(ServiceStatus.NoContent204));
        var controller = CreateController(service);

        var result = await controller.DeleteModifier("projPuid", "modPuid");

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(204, status.StatusCode);
    }

    [Fact]
    public async Task DeleteModifier_ServiceReturnsError_ReturnsErrorStatus()
    {
        var service = A.Fake<IModifierService>();
        A.CallTo(() => service.DeleteModifier("projPuid", "modPuid")).Returns(ServiceResult.Fail(ServiceStatus.NotFound404, "Not Found"));
        var controller = CreateController(service);

        var result = await controller.DeleteModifier("projPuid", "modPuid");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, obj.StatusCode);
    }
}
