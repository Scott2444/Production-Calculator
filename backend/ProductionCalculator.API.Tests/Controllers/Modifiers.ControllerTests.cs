using System.Diagnostics.CodeAnalysis;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.API.Controllers;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.API.Tests;

[ExcludeFromCodeCoverage]
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

    private static ModifierResponse CreateModifierResponse(string puid = "modPuid", string name = "Modifier")
    {
        return new ModifierResponse
        {
            Puid = puid,
            Name = name,
            Description = "desc",
            FlatBonus = 1.0,
            PercentBonus = 2.0,
            MultiplicativeBonus = 3.0,
            InputPercent = 1.0,
            OutputPercent = 1.0,
            Attributes =
            [
                new ModifierAttributeResponse
                {
                    Puid = "attr1",
                    FlatBonus = 4.0,
                    PercentBonus = 5.0,
                    MultiplicativeBonus = 6.0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            ],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetModifierByPuid_ValidRequest_Returns200OkWithResponse()
    {
        var service = A.Fake<IModifierService>();
        var modifier = CreateModifierResponse();
        A.CallTo(() => service.GetModifierByPuid("projPuid", "modPuid")).Returns(ServiceResult<ModifierResponse>.SuccessResult(modifier));
        var controller = CreateController(service);

        var result = await controller.GetModifierByPuid("projPuid", "modPuid");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        var response = Assert.IsType<ModifierResponse>(obj.Value);
        Assert.Equal("modPuid", response.Puid);
        Assert.Single(response.Attributes);
        Assert.Equal("attr1", response.Attributes[0].Puid);
    }

    [Fact]
    public async Task GetModifierByPuid_ModifierNotFound_Returns404NotFound()
    {
        var service = A.Fake<IModifierService>();
        A.CallTo(() => service.GetModifierByPuid("projPuid", "missing")).Returns(ServiceResult<ModifierResponse>.Fail(ServiceStatus.NotFound404, "Not Found"));
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
            .Returns(ServiceResult<ModifierResponse>.Redirection(ServiceStatus.SeeOther303, "/projects/canonical/modifiers/modPuid"));
        var controller = CreateController(service);

        var result = await controller.GetModifierByPuid("alias", "modPuid");

        var obj = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(303, obj.StatusCode);
        Assert.Equal("/projects/canonical/modifiers/modPuid", controller.Response.Headers["Location"]);
    }

    [Fact]
    public async Task GetModifiersByProjectPuid_ValidRequest_Returns200OkWithList()
    {
        var service = A.Fake<IModifierService>();
        var modifiers = new List<ModifierResponse> { CreateModifierResponse(puid: "p1"), CreateModifierResponse(puid: "p2") };
        A.CallTo(() => service.GetModifiersByProjectPuid("projPuid")).Returns(ServiceResult<List<ModifierResponse>>.SuccessResult(modifiers));
        var controller = CreateController(service);

        var result = await controller.GetModifiersByProjectPuid("projPuid");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        var response = Assert.IsType<List<ModifierResponse>>(obj.Value);
        Assert.Equal(2, response.Count);
        Assert.All(response, r => Assert.Single(r.Attributes));
    }

    [Fact]
    public async Task GetModifiersByProjectPuid_ProjectNotFound_Returns404NotFound()
    {
        var service = A.Fake<IModifierService>();
        A.CallTo(() => service.GetModifiersByProjectPuid("missing")).Returns(ServiceResult<List<ModifierResponse>>.Fail(ServiceStatus.NotFound404, "Not Found"));
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
            .Returns(ServiceResult<List<ModifierResponse>>.Redirection(ServiceStatus.SeeOther303, "/projects/canonical/modifiers"));
        var controller = CreateController(service);

        var result = await controller.GetModifiersByProjectPuid("alias");

        var obj = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(303, obj.StatusCode);
        Assert.Equal("/projects/canonical/modifiers", controller.Response.Headers["Location"]);
    }

    [Fact]
    public async Task AddModifier_ValidRequest_Returns201Created()
    {
        var service = A.Fake<IModifierService>();
        var modifier = CreateModifierResponse();
        var req = new ModifierRequest { Name = "New", Description = "Desc", FlatBonus = 1, PercentBonus = 2, MultiplicativeBonus = 3, InputPercent = 1, OutputPercent = 1, Attributes = [] };
        A.CallTo(() => service.AddModifier("projPuid", req.Name, req.Description, req.FlatBonus, req.PercentBonus, req.MultiplicativeBonus, req.InputPercent, req.OutputPercent, req.Attributes))
            .Returns(ServiceResult<ModifierResponse>.SuccessResult(modifier, ServiceStatus.Created201));
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
        var req = new ModifierRequest { Name = "Bad", Description = "Desc", FlatBonus = 1, PercentBonus = 2, MultiplicativeBonus = 3, InputPercent = 1, OutputPercent = 1, Attributes = [] };
        A.CallTo(() => service.AddModifier("projPuid", req.Name, req.Description, req.FlatBonus, req.PercentBonus, req.MultiplicativeBonus, req.InputPercent, req.OutputPercent, req.Attributes))
            .Returns(ServiceResult<ModifierResponse>.Fail(ServiceStatus.BadRequest400, "Bad Request"));
        var controller = CreateController(service);

        var result = await controller.AddModifier("projPuid", req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, obj.StatusCode);
    }

    [Fact]
    public async Task UpdateModifier_ValidRequest_Returns200OkWithResponse()
    {
        var service = A.Fake<IModifierService>();
        var modifier = CreateModifierResponse();
        var req = new ModifierRequest { Name = "Updated", Description = "Desc", FlatBonus = 1, PercentBonus = 2, MultiplicativeBonus = 3, InputPercent = 1, OutputPercent = 1, Attributes = [] };
        A.CallTo(() => service.UpdateModifier("projPuid", "modPuid", req.Name, req.Description, req.FlatBonus, req.PercentBonus, req.MultiplicativeBonus, req.InputPercent, req.OutputPercent, req.Attributes))
            .Returns(ServiceResult<ModifierResponse>.SuccessResult(modifier, ServiceStatus.Ok200));
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
        var req = new ModifierRequest { Name = "Bad", Description = "Desc", FlatBonus = 1, PercentBonus = 2, MultiplicativeBonus = 3, InputPercent = 1, OutputPercent = 1, Attributes = [] };
        A.CallTo(() => service.UpdateModifier("projPuid", "modPuid", req.Name, req.Description, req.FlatBonus, req.PercentBonus, req.MultiplicativeBonus, req.InputPercent, req.OutputPercent, req.Attributes))
            .Returns(ServiceResult<ModifierResponse>.Fail(ServiceStatus.Conflict409, "Conflict"));
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
