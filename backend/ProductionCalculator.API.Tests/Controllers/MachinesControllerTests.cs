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
public class MachineControllerTests
{
    private static MachinesController CreateController(IMachineService service)
    {
        var controller = new MachinesController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return controller;
    }

    private static MachineResponse CreateMachineResponse(string puid = "machPuid", string name = "Machine")
    {
        return new MachineResponse
        {
            Puid = puid,
            Name = name,
            Description = "desc",
            BaseSpeed = 10.0,
            RecipePuids = new List<string>(),
            Attributes = new List<AttributeRateExchange>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetMachineByPuid_ValidRequest_Returns200OkWithResponse()
    {
        var service = A.Fake<IMachineService>();
        var machine = CreateMachineResponse();
        A.CallTo(() => service.GetMachineByPuid("projPuid", "machPuid")).Returns(ServiceResult<MachineResponse>.SuccessResult(machine));
        var controller = CreateController(service);

        var result = await controller.GetMachineByPuid("projPuid", "machPuid");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        var response = Assert.IsType<MachineResponse>(obj.Value);
        Assert.Equal("machPuid", response.Puid);
    }

    [Fact]
    public async Task GetMachineByPuid_MachineNotFound_Returns404NotFound()
    {
        var service = A.Fake<IMachineService>();
        A.CallTo(() => service.GetMachineByPuid("projPuid", "missing")).Returns(ServiceResult<MachineResponse>.Fail(ServiceStatus.NotFound404, "Not Found"));
        var controller = CreateController(service);

        var result = await controller.GetMachineByPuid("projPuid", "missing");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, obj.StatusCode);
    }

    [Fact]
    public async Task GetMachinesByProjectPuid_ValidRequest_Returns200OkWithList()
    {
        var service = A.Fake<IMachineService>();
        var machines = new List<MachineResponse> { CreateMachineResponse(puid: "m1"), CreateMachineResponse(puid: "m2") };
        A.CallTo(() => service.GetMachinesByProjectPuid("projPuid")).Returns(ServiceResult<List<MachineResponse>>.SuccessResult(machines));
        var controller = CreateController(service);

        var result = await controller.GetMachinesByProjectPuid("projPuid");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        var response = Assert.IsType<List<MachineResponse>>(obj.Value);
        Assert.Equal(2, response.Count);
    }

    [Fact]
    public async Task GetMachinesByProjectPuid_ProjectNotFound_Returns404NotFound()
    {
        var service = A.Fake<IMachineService>();
        A.CallTo(() => service.GetMachinesByProjectPuid("missing")).Returns(ServiceResult<List<MachineResponse>>.Fail(ServiceStatus.NotFound404, "Not Found"));
        var controller = CreateController(service);

        var result = await controller.GetMachinesByProjectPuid("missing");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, obj.StatusCode);
    }

    [Fact]
    public async Task AddMachine_ValidRequest_Returns201Created()
    {
        var service = A.Fake<IMachineService>();
        var req = new MachineRequest { Name = "New Mach", BaseSpeed = 10.0, RecipePuids = new List<string>(), Attributes = new List<AttributeRateExchange>() };
        var machine = CreateMachineResponse(name: "New Mach");
        A.CallTo(() => service.AddMachine("projPuid", "New Mach", A<string>._, 10.0, A<List<string>>._, A<List<AttributeRateExchange>>._)).Returns(ServiceResult<MachineResponse>.SuccessResult(machine, ServiceStatus.Created201));
        var controller = CreateController(service);

        var result = await controller.AddMachine("projPuid", req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, obj.StatusCode);
    }

    [Fact]
    public async Task AddMachine_ServiceReturnsError_ReturnsErrorStatus()
    {
        var service = A.Fake<IMachineService>();
        var req = new MachineRequest { Name = "New Mach", BaseSpeed = 10.0, RecipePuids = new List<string>(), Attributes = new List<AttributeRateExchange>() };
        A.CallTo(() => service.AddMachine("projPuid", "New Mach", A<string>._, 10.0, A<List<string>>._, A<List<AttributeRateExchange>>._)).Returns(ServiceResult<MachineResponse>.Fail(ServiceStatus.BadRequest400, "Error"));
        var controller = CreateController(service);

        var result = await controller.AddMachine("projPuid", req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, obj.StatusCode);
    }

    [Fact]
    public async Task UpdateMachine_ValidRequest_Returns200OkWithResponse()
    {
        var service = A.Fake<IMachineService>();
        var req = new MachineRequest { Name = "Updated Mach", BaseSpeed = 15.0, RecipePuids = new List<string>(), Attributes = new List<AttributeRateExchange>() };
        var machine = CreateMachineResponse(name: "Updated Mach");
        A.CallTo(() => service.UpdateMachine("projPuid", "machPuid", "Updated Mach", A<string>._, 15.0, A<List<string>>._, A<List<AttributeRateExchange>>._)).Returns(ServiceResult<MachineResponse>.SuccessResult(machine));
        var controller = CreateController(service);

        var result = await controller.UpdateMachine("projPuid", "machPuid", req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
    }

    [Fact]
    public async Task UpdateMachine_ServiceReturnsError_ReturnsErrorStatus()
    {
        var service = A.Fake<IMachineService>();
        var req = new MachineRequest { Name = "Updated Mach", BaseSpeed = 15.0, RecipePuids = new List<string>(), Attributes = new List<AttributeRateExchange>() };
        A.CallTo(() => service.UpdateMachine("projPuid", "machPuid", "Updated Mach", A<string>._, 15.0, A<List<string>>._, A<List<AttributeRateExchange>>._)).Returns(ServiceResult<MachineResponse>.Fail(ServiceStatus.Conflict409, "Conflict"));
        var controller = CreateController(service);

        var result = await controller.UpdateMachine("projPuid", "machPuid", req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(409, obj.StatusCode);
    }

    [Fact]
    public async Task DeleteMachine_ValidRequest_Returns204NoContent()
    {
        var service = A.Fake<IMachineService>();
        A.CallTo(() => service.DeleteMachine("projPuid", "machPuid")).Returns(ServiceResult.SuccessResult(ServiceStatus.NoContent204));
        var controller = CreateController(service);

        var result = await controller.DeleteMachine("projPuid", "machPuid");

        var obj = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(204, obj.StatusCode);
    }

    [Fact]
    public async Task DeleteMachine_ServiceReturnsError_ReturnsErrorStatus()
    {
        var service = A.Fake<IMachineService>();
        A.CallTo(() => service.DeleteMachine("projPuid", "machPuid")).Returns(ServiceResult.Fail(ServiceStatus.NotFound404, "Not Found"));
        var controller = CreateController(service);

        var result = await controller.DeleteMachine("projPuid", "machPuid");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, obj.StatusCode);
    }
}
