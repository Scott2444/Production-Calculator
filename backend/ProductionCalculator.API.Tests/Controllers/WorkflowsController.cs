using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.API.Controllers;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.API.Tests;

public class WorkflowsControllerTests
{
    private static WorkflowsController CreateController(IWorkflowService service)
    {
        var controller = new WorkflowsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return controller;
    }

    private static Workflow CreateWorkflow(string puid = "wfPuid", string name = "Workflow")
    {
        return new Workflow
        {
            Workflow_Id = 1,
            Project_Id = 1,
            Puid = puid,
            Name = name,
            Description = "desc",
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetWorkflowByPuid_ValidRequest_Returns200OkWithResponse()
    {
        var service = A.Fake<IWorkflowService>();
        var workflow = CreateWorkflow();
        A.CallTo(() => service.GetWorkflowByPuid("projPuid", "wfPuid")).Returns(ServiceResult<Workflow>.SuccessResult(workflow));
        var controller = CreateController(service);

        var result = await controller.GetWorkflowByPuid("projPuid", "wfPuid");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        var response = Assert.IsType<WorkflowResponse>(obj.Value);
        Assert.Equal("wfPuid", response.Puid);
    }

    [Fact]
    public async Task GetWorkflowByPuid_WorkflowNotFound_Returns404NotFound()
    {
        var service = A.Fake<IWorkflowService>();
        A.CallTo(() => service.GetWorkflowByPuid("projPuid", "missing")).Returns(ServiceResult<Workflow>.Fail(ServiceStatus.NotFound404, "Not Found"));
        var controller = CreateController(service);

        var result = await controller.GetWorkflowByPuid("projPuid", "missing");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, obj.StatusCode);
    }

    [Fact]
    public async Task GetWorkflowsByProjectPuid_ValidRequest_Returns200OkWithList()
    {
        var service = A.Fake<IWorkflowService>();
        var workflows = new List<Workflow> { CreateWorkflow(puid: "w1"), CreateWorkflow(puid: "w2") };
        A.CallTo(() => service.GetWorkflowsByProjectPuid("projPuid")).Returns(ServiceResult<List<Workflow>>.SuccessResult(workflows));
        var controller = CreateController(service);

        var result = await controller.GetWorkflowsByProjectPuid("projPuid");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        var response = Assert.IsType<List<WorkflowResponse>>(obj.Value);
        Assert.Equal(2, response.Count);
    }

    [Fact]
    public async Task GetWorkflowsByProjectPuid_ProjectNotFound_Returns404NotFound()
    {
        var service = A.Fake<IWorkflowService>();
        A.CallTo(() => service.GetWorkflowsByProjectPuid("missing")).Returns(ServiceResult<List<Workflow>>.Fail(ServiceStatus.NotFound404, "Not Found"));
        var controller = CreateController(service);

        var result = await controller.GetWorkflowsByProjectPuid("missing");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, obj.StatusCode);
    }

    [Fact]
    public async Task AddWorkflow_ValidRequest_Returns201Created()
    {
        var service = A.Fake<IWorkflowService>();
        var req = new WorkflowRequest { Name = "NewWF", Description = "desc" };
        var workflow = CreateWorkflow(puid: "new-puid", name: "NewWF");
        A.CallTo(() => service.AddWorkflow("projPuid", "NewWF", "desc")).Returns(ServiceResult<Workflow>.SuccessResult(workflow, ServiceStatus.Created201));
        var controller = CreateController(service);

        var result = await controller.AddWorkflow("projPuid", req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, obj.StatusCode);
        var response = Assert.IsType<WorkflowResponse>(obj.Value);
        Assert.Equal("new-puid", response.Puid);
    }

    [Fact]
    public async Task AddWorkflow_ServiceReturnsError_ReturnsErrorStatus()
    {
        var service = A.Fake<IWorkflowService>();
        var req = new WorkflowRequest { Name = "Duplicate" };
        A.CallTo(() => service.AddWorkflow("projPuid", "Duplicate", A<string?>._)).Returns(ServiceResult<Workflow>.Fail(ServiceStatus.Conflict409, "Conflict"));
        var controller = CreateController(service);

        var result = await controller.AddWorkflow("projPuid", req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(409, obj.StatusCode);
    }

    [Fact]
    public async Task UpdateWorkflow_ValidRequest_Returns200OkWithResponse()
    {
        var service = A.Fake<IWorkflowService>();
        var req = new WorkflowRequest { Name = "UpdatedName" };
        var workflow = CreateWorkflow(name: "UpdatedName");
        A.CallTo(() => service.UpdateWorkflow("projPuid", "wfPuid", "UpdatedName", A<string?>._)).Returns(ServiceResult<Workflow>.SuccessResult(workflow));
        var controller = CreateController(service);

        var result = await controller.UpdateWorkflow("projPuid", "wfPuid", req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        var response = Assert.IsType<WorkflowResponse>(obj.Value);
        Assert.Equal("UpdatedName", response.Name);
    }

    [Fact]
    public async Task UpdateWorkflow_ServiceReturnsError_ReturnsErrorStatus()
    {
        var service = A.Fake<IWorkflowService>();
        var req = new WorkflowRequest { Name = "Invalid" };
        A.CallTo(() => service.UpdateWorkflow("projPuid", "wfPuid", "Invalid", A<string?>._)).Returns(ServiceResult<Workflow>.Fail(ServiceStatus.BadRequest400, "Bad Request"));
        var controller = CreateController(service);

        var result = await controller.UpdateWorkflow("projPuid", "wfPuid", req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, obj.StatusCode);
    }

    [Fact]
    public async Task DeleteWorkflow_ValidRequest_Returns204NoContent()
    {
        var service = A.Fake<IWorkflowService>();
        A.CallTo(() => service.DeleteWorkflow("projPuid", "wfPuid")).Returns(ServiceResult.SuccessResult(ServiceStatus.NoContent204));
        var controller = CreateController(service);

        var result = await controller.DeleteWorkflow("projPuid", "wfPuid");

        var obj = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(204, obj.StatusCode);
    }

    [Fact]
    public async Task DeleteWorkflow_ServiceReturnsError_ReturnsErrorStatus()
    {
        var service = A.Fake<IWorkflowService>();
        A.CallTo(() => service.DeleteWorkflow("projPuid", "wfPuid")).Returns(ServiceResult.Fail(ServiceStatus.NotFound404, "Not Found"));
        var controller = CreateController(service);

        var result = await controller.DeleteWorkflow("projPuid", "wfPuid");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, obj.StatusCode);
    }

    [Fact]
    public async Task UpdateTargetDemand_ValidRequest_Returns200OkWithResponse()
    {
        var service = A.Fake<IWorkflowService>();
        var req = new WorkflowTargetRequest { Targets = [new WorkflowTargetExchange { ProductPuid = "p1", TargetRate = 10.0 }] };
        var chart = new WorkflowChartResponse { Nodes = [], Edges = [], Targets = [], ProductNodes = [] };
        A.CallTo(() => service.UpdateTargetDemand("projPuid", "wfPuid", A<List<(string, double)>>._)).Returns(ServiceResult<WorkflowChartResponse>.SuccessResult(chart));
        var controller = CreateController(service);

        var result = await controller.UpdateTargetDemand("projPuid", "wfPuid", req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        Assert.IsType<WorkflowChartResponse>(obj.Value);
    }

    [Fact]
    public async Task UpdateTargetDemand_ServiceReturnsError_ReturnsErrorStatus()
    {
        var service = A.Fake<IWorkflowService>();
        var req = new WorkflowTargetRequest();
        A.CallTo(() => service.UpdateTargetDemand("projPuid", "wfPuid", A<List<(string, double)>>._)).Returns(ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.BadRequest400, "Error"));
        var controller = CreateController(service);

        var result = await controller.UpdateTargetDemand("projPuid", "wfPuid", req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, obj.StatusCode);
    }
}
