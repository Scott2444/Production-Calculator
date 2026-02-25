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
public class ResolveControllerTests
{
	private static ResolveController CreateController(IProjectService service)
	{
		var controller = new ResolveController(service)
		{
			ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext()
			}
		};
		return controller;
	}

    private static Project CreateProject(string puid = "projPuid", string name = "Project")
    {
        return new Project
        {
            Project_Id = 1,
            User_Id = 1,
            Puid = puid,
            Name = name,
            Description = "desc",
            Is_Public = false,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

	[Fact]
	public async Task ResolveProject_ValidRequest_ReturnsOk()
	{
		var service = A.Fake<IProjectService>();
		var project = CreateProject("projPuid");
		A.CallTo(() => service.ResolveProject("user", "project"))
			.Returns(ServiceResult<Project>.SuccessResult(project, ServiceStatus.Ok200));
		var controller = CreateController(service);

		var result = await controller.ResolveProject("user", "project");

		var obj = Assert.IsType<ObjectResult>(result);
		Assert.Equal(200, obj.StatusCode);
		var response = Assert.IsType<ProjectResolveResponse>(obj.Value);
		Assert.Equal("projPuid", response.ProjectPuid);
	}

	[Fact]
	public async Task ResolveProject_NotFound_ReturnsNotFound()
	{
		var service = A.Fake<IProjectService>();
		A.CallTo(() => service.ResolveProject("user", "project"))
			.Returns(ServiceResult<Project>.Fail(ServiceStatus.NotFound404, "Not Found"));
		var controller = CreateController(service);

		var result = await controller.ResolveProject("user", "project");

		var obj = Assert.IsType<ObjectResult>(result);
		Assert.Equal(404, obj.StatusCode);
	}

	[Fact]
	public async Task ResolveProject_BadRequest_ReturnsBadRequest()
	{
		var service = A.Fake<IProjectService>();
		A.CallTo(() => service.ResolveProject("user", "project"))
			.Returns(ServiceResult<Project>.Fail(ServiceStatus.BadRequest400, "Bad Request"));
		var controller = CreateController(service);

		var result = await controller.ResolveProject("user", "project");

		var obj = Assert.IsType<ObjectResult>(result);
		Assert.Equal(400, obj.StatusCode);
	}
}
