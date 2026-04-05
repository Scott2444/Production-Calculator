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
public class UsersControllerTests
{
    private static UsersController CreateController(IUserService service)
    {
        var controller = new UsersController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return controller;
    }

    private static User CreateUser(string puid = "user123456")
    {
        return new User
        {
            User_Id = 1,
            Username = "user",
            Email = "user@example.com",
            Password_Hash = "hash",
            Role_Id = 1,
            Puid = puid,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    private static Project CreateProject(string puid = "proj123456")
    {
        return new Project
        {
            Project_Id = 1,
            User_Id = 1,
            Puid = puid,
            Name = "proj",
            Description = "d",
            Is_Public = false,
            Alias_Project_Puid = null,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    private static string? TryGetError(object? value)
    {
        if (value == null) return null;
        var prop = value.GetType().GetProperty("error");
        return prop?.GetValue(value)?.ToString();
    }

    [Fact]
    public async Task Register_ValidRequest_Returns201Created()
    {
        var service = A.Fake<IUserService>();
        var controller = CreateController(service);
        var user = CreateUser();

        A.CallTo(() => service.Register("user", "user@example.com", "password"))
            .Returns(ServiceResult<User>.SuccessResult(user, ServiceStatus.Created201));

        var result = await controller.Register(new RegisterUserRequest { Username = "user", Email = "user@example.com", Password = "password" });

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, obj.StatusCode);
        Assert.IsType<UserResponse>(obj.Value);
    }

    [Fact]
    public async Task Register_ServiceReturnsError_Returns400BadRequest()
    {
        var service = A.Fake<IUserService>();
        var controller = CreateController(service);

        A.CallTo(() => service.Register(A<string>._, A<string>._, A<string>._))
            .Returns(ServiceResult<User>.Fail(ServiceStatus.BadRequest400, "bad"));

        var result = await controller.Register(new RegisterUserRequest { Username = "u", Email = "e", Password = "p" });

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, obj.StatusCode);
        Assert.Equal("bad", TryGetError(obj.Value));
    }

    [Fact]
    public async Task ValidateNewUser_UniqueUsernameAndEmail_Returns200Ok()
    {
        var service = A.Fake<IUserService>();
        var controller = CreateController(service);

        A.CallTo(() => service.ValidateNewUser("user", "user@example.com"))
            .Returns(ServiceResult.SuccessResult(ServiceStatus.Ok200));

        var result = await controller.ValidateNewUser(new ValidateNewUserRequest { Username = "user", Email = "user@example.com" });

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(200, status.StatusCode);
    }

    [Fact]
    public async Task ValidateNewUser_DuplicateOrInvalidData_Returns400BadRequest()
    {
        var service = A.Fake<IUserService>();
        var controller = CreateController(service);

        A.CallTo(() => service.ValidateNewUser(A<string>._, A<string>._))
            .Returns(ServiceResult.Fail(ServiceStatus.BadRequest400, "bad"));

        var result = await controller.ValidateNewUser(new ValidateNewUserRequest { Username = "", Email = "" });

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, obj.StatusCode);
    }

    [Fact]
    public async Task GetByPuid_UserExists_Returns200OkWithResponse()
    {
        var service = A.Fake<IUserService>();
        var controller = CreateController(service);
        var user = CreateUser("user123456");

        A.CallTo(() => service.GetUserByPuid("user123456"))
            .Returns(ServiceResult<(User, bool)>.SuccessResult((user, true), ServiceStatus.Ok200));

        var result = await controller.GetByPuid("user123456");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        var response = Assert.IsType<UserResponse>(obj.Value);
        Assert.Equal("user123456", response.Puid);
        Assert.True(response.IsVerified);
    }

    [Fact]
    public async Task GetByPuid_UserNotFound_Returns404NotFound()
    {
        var service = A.Fake<IUserService>();
        var controller = CreateController(service);

        A.CallTo(() => service.GetUserByPuid("missing"))
            .Returns(ServiceResult<(User, bool)>.Fail(ServiceStatus.NotFound404, "no"));

        var result = await controller.GetByPuid("missing");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, obj.StatusCode);
    }

    [Fact]
    public async Task GetByPuid_ServiceError_Returns400BadRequest()
    {
        var service = A.Fake<IUserService>();
        var controller = CreateController(service);

        A.CallTo(() => service.GetUserByPuid(A<string>._))
            .Returns(ServiceResult<(User, bool)>.Fail(ServiceStatus.BadRequest400, "bad"));

        var result = await controller.GetByPuid("user123456");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, obj.StatusCode);
    }

    [Fact]
    public async Task GetProjectsByUserPuid_ProjectsFound_Returns200OkWithList()
    {
        var userService = A.Fake<IUserService>();
        var controller = CreateController(userService);
        var projectService = A.Fake<IProjectService>();
        var projects = new List<ProjectResponse> 
        { 
            new ProjectResponse { Puid = "proj123456", Name = "P1", OwnerUsername = "u", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new ProjectResponse { Puid = "proj234567", Name = "P2", OwnerUsername = "u", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        A.CallTo(() => projectService.GetProjectsByUserPuid("user123456"))
            .Returns(ServiceResult<List<ProjectResponse>>.SuccessResult(projects, ServiceStatus.Ok200));

        var result = await controller.GetProjectsByUserPuid("user123456", projectService);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        var list = Assert.IsType<List<ProjectResponse>>(obj.Value);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task GetProjectsByUserPuid_UserNotFound_Returns404NotFound()
    {
        var userService = A.Fake<IUserService>();
        var controller = CreateController(userService);
        var projectService = A.Fake<IProjectService>();

        A.CallTo(() => projectService.GetProjectsByUserPuid("user123456"))
            .Returns(ServiceResult<List<ProjectResponse>>.Fail(ServiceStatus.NotFound404, "no"));

        var result = await controller.GetProjectsByUserPuid("user123456", projectService);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, obj.StatusCode);
    }

    [Fact]
    public async Task GetProjectsByUserPuid_ServiceError_Returns400BadRequest()
    {
        var userService = A.Fake<IUserService>();
        var controller = CreateController(userService);
        var projectService = A.Fake<IProjectService>();

        A.CallTo(() => projectService.GetProjectsByUserPuid(A<string>._))
            .Returns(ServiceResult<List<ProjectResponse>>.Fail(ServiceStatus.BadRequest400, "bad"));

        var result = await controller.GetProjectsByUserPuid("user123456", projectService);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, obj.StatusCode);
    }

    [Fact]
    public async Task DeleteByPuid_UserExists_Returns204NoContent()
    {
        var service = A.Fake<IUserService>();
        var controller = CreateController(service);

        A.CallTo(() => service.DeleteUserById("user123456"))
            .Returns(ServiceResult.SuccessResult(ServiceStatus.NoContent204));

        var result = await controller.DeleteByPuid("user123456");

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(204, status.StatusCode);
    }

    [Fact]
    public async Task DeleteByPuid_UserNotFound_Returns404NotFound()
    {
        var service = A.Fake<IUserService>();
        var controller = CreateController(service);

        A.CallTo(() => service.DeleteUserById("user123456"))
            .Returns(ServiceResult.Fail(ServiceStatus.NotFound404, "no"));

        var result = await controller.DeleteByPuid("user123456");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, obj.StatusCode);
    }

    [Fact]
    public async Task DeleteByPuid_ServiceError_Returns400BadRequest()
    {
        var service = A.Fake<IUserService>();
        var controller = CreateController(service);

        A.CallTo(() => service.DeleteUserById(A<string>._))
            .Returns(ServiceResult.Fail(ServiceStatus.BadRequest400, "bad"));

        var result = await controller.DeleteByPuid("user123456");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, obj.StatusCode);
    }
}
