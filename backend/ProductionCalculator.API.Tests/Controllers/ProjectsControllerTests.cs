using System.Diagnostics.CodeAnalysis;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.API.Controllers;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.API.Tests;

[ExcludeFromCodeCoverage]
public class ProjectsControllerTests
{
    private static ProjectsController CreateController(IProjectService service)
    {
        var controller = new ProjectsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return controller;
    }

    private static ProjectResponse CreateProjectResponse(string puid = "projPuid", string name = "Project")
    {
        return new ProjectResponse
        {
            Puid = puid,
            Name = name,
            OwnerUsername = "testuser",
            Description = "desc",
            IsPublic = false,
            AliasCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static PublicProjectSearchPageResponse CreateSearchResponse()
    {
        return new PublicProjectSearchPageResponse
        {
            Projects = new List<ProjectResponse> { CreateProjectResponse(name: "Found") },
            Page = 1,
            PageSize = 20,
            TotalCount = 1,
            TotalPages = 1
        };
    }

    [Fact]
    public async Task AddProject_ValidRequest_ReturnsCreated()
    {
        var service = A.Fake<IProjectService>();
        var project = CreateProjectResponse();
        A.CallTo(() => service.AddProject(A<string>._, A<string>._, A<bool?>._, A<string>._))
            .Returns(ServiceResult<ProjectResponse>.SuccessResult(project, ServiceStatus.Created201));
        var controller = CreateController(service);
        var request = new ProjectRequest { Name = "Project" };

        var result = await controller.AddProject(request);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, obj.StatusCode);
        Assert.IsType<ProjectResponse>(obj.Value);
    }

    [Fact]
    public async Task AddProject_InvalidRequest_ReturnsBadRequest()
    {
        var service = A.Fake<IProjectService>();
        A.CallTo(() => service.AddProject(A<string>._, A<string>._, A<bool?>._, A<string>._))
            .Returns(ServiceResult<ProjectResponse>.Fail(ServiceStatus.BadRequest400, "Error"));
        var controller = CreateController(service);
        var request = new ProjectRequest { Name = "" };

        var result = await controller.AddProject(request);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, obj.StatusCode);
    }

    [Fact]
    public async Task UpdateProject_ValidRequest_ReturnsOk()
    {
        var service = A.Fake<IProjectService>();
        var project = CreateProjectResponse();
        A.CallTo(() => service.UpdateProject(A<string>._, A<string>._, A<string>._, A<bool?>._, A<string>._))
            .Returns(ServiceResult<ProjectResponse>.SuccessResult(project, ServiceStatus.Ok200));
        var controller = CreateController(service);
        var request = new ProjectRequest { Name = "Updated" };

        var result = await controller.UpdateProject("projPuid", request);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        Assert.IsType<ProjectResponse>(obj.Value);
    }

    [Fact]
    public async Task UpdateProject_InvalidRequest_ReturnsBadRequest()
    {
        var service = A.Fake<IProjectService>();
        A.CallTo(() => service.UpdateProject(A<string>._, A<string>._, A<string>._, A<bool?>._, A<string>._))
            .Returns(ServiceResult<ProjectResponse>.Fail(ServiceStatus.BadRequest400, "Error"));
        var controller = CreateController(service);
        var request = new ProjectRequest { Name = "" };

        var result = await controller.UpdateProject("projPuid", request);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, obj.StatusCode);
    }

    [Fact]
    public async Task UpdateProject_NotFound_ReturnsNotFound()
    {
        var service = A.Fake<IProjectService>();
        A.CallTo(() => service.UpdateProject(A<string>._, A<string>._, A<string>._, A<bool?>._, A<string>._))
            .Returns(ServiceResult<ProjectResponse>.Fail(ServiceStatus.NotFound404, "Not Found"));
        var controller = CreateController(service);
        var request = new ProjectRequest { Name = "Name" };

        var result = await controller.UpdateProject("projPuid", request);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, obj.StatusCode);
    }

    [Fact]
    public async Task GetProjectByPuid_ValidRequest_ReturnsOk()
    {
        var service = A.Fake<IProjectService>();
        var project = CreateProjectResponse();
        A.CallTo(() => service.GetProjectByPuid("projPuid"))
            .Returns(ServiceResult<ProjectResponse>.SuccessResult(project, ServiceStatus.Ok200));
        var controller = CreateController(service);

        var result = await controller.GetProjectByPuid("projPuid");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        Assert.IsType<ProjectResponse>(obj.Value);
    }

    [Fact]
    public async Task GetProjectByPuid_NotFound_ReturnsNotFound()
    {
        var service = A.Fake<IProjectService>();
        A.CallTo(() => service.GetProjectByPuid("projPuid"))
            .Returns(ServiceResult<ProjectResponse>.Fail(ServiceStatus.NotFound404, "Not Found"));
        var controller = CreateController(service);

        var result = await controller.GetProjectByPuid("projPuid");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, obj.StatusCode);
    }

    [Fact]
    public async Task SearchPublicProjects_ValidRequest_ReturnsOk()
    {
        var service = A.Fake<IProjectService>();
        var searchResult = CreateSearchResponse();
        A.CallTo(() => service.SearchPublicProjects("iron", 1, 20))
            .Returns(ServiceResult<PublicProjectSearchPageResponse>.SuccessResult(searchResult, ServiceStatus.Ok200));
        var controller = CreateController(service);

        var result = await controller.SearchPublicProjects("iron", 1, 20);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        Assert.IsType<PublicProjectSearchPageResponse>(obj.Value);
    }

    [Fact]
    public async Task SearchPublicProjects_InvalidRequest_ReturnsBadRequest()
    {
        var service = A.Fake<IProjectService>();
        A.CallTo(() => service.SearchPublicProjects(A<string>._, A<int>._, A<int>._))
            .Returns(ServiceResult<PublicProjectSearchPageResponse>.Fail(ServiceStatus.BadRequest400, "Search query is required."));
        var controller = CreateController(service);

        var result = await controller.SearchPublicProjects("", 1, 20);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, obj.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_ValidRequest_ReturnsNoContent()
    {
        var service = A.Fake<IProjectService>();
        A.CallTo(() => service.DeleteProject("projPuid"))
            .Returns(ServiceResult.SuccessResult(ServiceStatus.NoContent204));
        var controller = CreateController(service);

        var result = await controller.DeleteProject("projPuid");

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(204, status.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_NotFound_ReturnsNotFound()
    {
        var service = A.Fake<IProjectService>();
        A.CallTo(() => service.DeleteProject("projPuid"))
            .Returns(ServiceResult.Fail(ServiceStatus.NotFound404, "Not Found"));
        var controller = CreateController(service);

        var result = await controller.DeleteProject("projPuid");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, obj.StatusCode);
    }
}
