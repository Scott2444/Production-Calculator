using System.Diagnostics.CodeAnalysis;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using ProductionCalculator.Business.Helpers;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Services;

namespace ProductionCalculator.Business.Tests;

[ExcludeFromCodeCoverage]
public class ProjectServiceTests
{
    private static User CreateUser(int id = 1, string puid = "userPuid")
    {
        return new User
        {
            User_Id = id,
            Username = "testuser",
            Email = "test@example.com",
            Password_Hash = "hash",
            Role_Id = 1,
            Puid = puid,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    private static Project CreateProject(int id = 1, int userId = 1, string puid = "projPuid", string name = "Project")
    {
        return new Project
        {
            Project_Id = id,
            User_Id = userId,
            Puid = puid,
            Name = name,
            Description = "desc",
            Is_Public = false,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    private static ProjectService CreateService(
        ICurrentUserService currentUser,
        IProjectRepository repo,
        IUserRepository userRepo)
    {
        var logger = A.Fake<ILogger<ProjectService>>();
        return new ProjectService(currentUser, repo, userRepo, logger);
    }

    // AddProject Tests
    [Fact]
    public async Task AddProject_EmptyName_ReturnsBadRequest()
    {
        var currentUser = A.Fake<ICurrentUserService>();
        var repo = A.Fake<IProjectRepository>();
        var userRepo = A.Fake<IUserRepository>();
        var service = CreateService(currentUser, repo, userRepo);

        var result = await service.AddProject("", "desc", false, null);

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task AddProject_UserNotFound_ReturnsBadRequest()
    {
        var currentUser = A.Fake<ICurrentUserService>();
        A.CallTo(() => currentUser.UserPuid).Returns("userPuid");
        var repo = A.Fake<IProjectRepository>();
        var userRepo = A.Fake<IUserRepository>();
        A.CallTo(() => userRepo.GetByPuid("userPuid")).Returns((User?)null);
        var service = CreateService(currentUser, repo, userRepo);

        var result = await service.AddProject("name", "desc", false, null);

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task AddProject_DuplicateName_ReturnsConflict()
    {
        var user = CreateUser();
        var currentUser = A.Fake<ICurrentUserService>();
        A.CallTo(() => currentUser.UserPuid).Returns(user.Puid);
        var repo = A.Fake<IProjectRepository>();
        var userRepo = A.Fake<IUserRepository>();
        A.CallTo(() => userRepo.GetByPuid(user.Puid)).Returns(user);
        A.CallTo(() => repo.GetProjectsByUserId(user.User_Id)).Returns(new List<Project> { CreateProject(name: "Dup") });
        var service = CreateService(currentUser, repo, userRepo);

        var result = await service.AddProject("Dup", "desc", false, null);

        Assert.Equal(ServiceStatus.Conflict409, result.Status);
    }

    [Fact]
    public async Task AddProject_InvalidAlias_ReturnsBadRequest()
    {
        var user = CreateUser();
        var currentUser = A.Fake<ICurrentUserService>();
        A.CallTo(() => currentUser.UserPuid).Returns(user.Puid);
        var repo = A.Fake<IProjectRepository>();
        var userRepo = A.Fake<IUserRepository>();
        A.CallTo(() => userRepo.GetByPuid(user.Puid)).Returns(user);
        A.CallTo(() => repo.GetProjectsByUserId(user.User_Id)).Returns(new List<Project>());
        A.CallTo(() => repo.GetProjectByPuid("alias")).Returns((Project?)null);
        var service = CreateService(currentUser, repo, userRepo);

        var result = await service.AddProject("name", "desc", false, "alias");

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task AddProject_ValidRequest_ReturnsCreated()
    {
        var user = CreateUser();
        var currentUser = A.Fake<ICurrentUserService>();
        A.CallTo(() => currentUser.UserPuid).Returns(user.Puid);
        var repo = A.Fake<IProjectRepository>();
        var userRepo = A.Fake<IUserRepository>();
        A.CallTo(() => userRepo.GetByPuid(user.Puid)).Returns(user);
        A.CallTo(() => repo.GetProjectsByUserId(user.User_Id)).Returns(new List<Project>());
        A.CallTo(() => repo.PuidExists(A<string>._)).Returns(false);
        var service = CreateService(currentUser, repo, userRepo);

        var result = await service.AddProject("name", "desc", false, null);

        Assert.Equal(ServiceStatus.Created201, result.Status);
        A.CallTo(() => repo.AddProject(A<Project>._)).MustHaveHappenedOnceExactly();
    }

    // UpdateProject Tests
    [Fact]
    public async Task UpdateProject_EmptyName_ReturnsBadRequest()
    {
        var currentUser = A.Fake<ICurrentUserService>();
        var repo = A.Fake<IProjectRepository>();
        var userRepo = A.Fake<IUserRepository>();
        var service = CreateService(currentUser, repo, userRepo);

        var result = await service.UpdateProject("puid", "", "desc", false, null);

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task UpdateProject_ProjectNotFound_ReturnsNotFound()
    {
        var user = CreateUser();
        var currentUser = A.Fake<ICurrentUserService>();
        A.CallTo(() => currentUser.UserPuid).Returns(user.Puid);
        var repo = A.Fake<IProjectRepository>();
        var userRepo = A.Fake<IUserRepository>();
        A.CallTo(() => userRepo.GetByPuid(user.Puid)).Returns(user);
        A.CallTo(() => repo.GetProjectByPuid("puid")).Returns((Project?)null);
        var service = CreateService(currentUser, repo, userRepo);

        var result = await service.UpdateProject("puid", "name", "desc", false, null);

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateProject_DuplicateNameOtherThanSelf_ReturnsConflict()
    {
        var user = CreateUser();
        var currentUser = A.Fake<ICurrentUserService>();
        A.CallTo(() => currentUser.UserPuid).Returns(user.Puid);
        var repo = A.Fake<IProjectRepository>();
        var userRepo = A.Fake<IUserRepository>();
        A.CallTo(() => userRepo.GetByPuid(user.Puid)).Returns(user);
        var project = CreateProject(id: 1, puid: "puid", name: "OldName");
        A.CallTo(() => repo.GetProjectByPuid("puid")).Returns(project);
        A.CallTo(() => repo.GetProjectsByUserId(user.User_Id)).Returns(new List<Project> { CreateProject(id: 2, name: "Dup") });
        var service = CreateService(currentUser, repo, userRepo);

        var result = await service.UpdateProject("puid", "Dup", "desc", false, null);

        Assert.Equal(ServiceStatus.Conflict409, result.Status);
    }

    [Fact]
    public async Task UpdateProject_ValidRequest_ReturnsOk()
    {
        var user = CreateUser();
        var currentUser = A.Fake<ICurrentUserService>();
        A.CallTo(() => currentUser.UserPuid).Returns(user.Puid);
        var repo = A.Fake<IProjectRepository>();
        var userRepo = A.Fake<IUserRepository>();
        A.CallTo(() => userRepo.GetByPuid(user.Puid)).Returns(user);
        var project = CreateProject(id: 1, puid: "puid");
        A.CallTo(() => repo.GetProjectByPuid("puid")).Returns(project);
        A.CallTo(() => repo.GetProjectsByUserId(user.User_Id)).Returns(new List<Project> { project });
        var service = CreateService(currentUser, repo, userRepo);

        var result = await service.UpdateProject("puid", "NewName", "desc", false, null);

        Assert.Equal(ServiceStatus.Ok200, result.Status);
        Assert.Equal("NewName", project.Name);
        A.CallTo(() => repo.UpdateProject(project)).MustHaveHappenedOnceExactly();
    }

    // GetProjectByPuid Tests
    [Fact]
    public async Task GetProjectByPuid_EmptyPuid_ReturnsBadRequest()
    {
        var service = CreateService(A.Fake<ICurrentUserService>(), A.Fake<IProjectRepository>(), A.Fake<IUserRepository>());

        var result = await service.GetProjectByPuid("");

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task GetProjectByPuid_NotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IProjectRepository>();
        A.CallTo(() => repo.GetProjectByPuid("puid")).Returns((Project?)null);
        var service = CreateService(A.Fake<ICurrentUserService>(), repo, A.Fake<IUserRepository>());

        var result = await service.GetProjectByPuid("puid");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetProjectByPuid_Valid_ReturnsProject()
    {
        var project = CreateProject(puid: "puid");
        var repo = A.Fake<IProjectRepository>();
        A.CallTo(() => repo.GetProjectByPuid("puid")).Returns(project);
        var service = CreateService(A.Fake<ICurrentUserService>(), repo, A.Fake<IUserRepository>());

        var result = await service.GetProjectByPuid("puid");

        Assert.Equal(ServiceStatus.Ok200, result.Status);
        Assert.Equal(project, result.Data);
    }

    // GetProjectsByUserPuid Tests
    [Fact]
    public async Task GetProjectsByUserPuid_EmptyPuid_ReturnsBadRequest()
    {
        var service = CreateService(A.Fake<ICurrentUserService>(), A.Fake<IProjectRepository>(), A.Fake<IUserRepository>());

        var result = await service.GetProjectsByUserPuid("");

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task GetProjectsByUserPuid_UserNotFound_ReturnsNotFound()
    {
        var userRepo = A.Fake<IUserRepository>();
        A.CallTo(() => userRepo.GetByPuid("puid")).Returns((User?)null);
        var service = CreateService(A.Fake<ICurrentUserService>(), A.Fake<IProjectRepository>(), userRepo);

        var result = await service.GetProjectsByUserPuid("puid");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetProjectsByUserPuid_Valid_ReturnsList()
    {
        var user = CreateUser(id: 1, puid: "uPuid");
        var userRepo = A.Fake<IUserRepository>();
        A.CallTo(() => userRepo.GetByPuid(user.Puid)).Returns(user);
        var projects = new List<Project> { CreateProject(userId: 1) };
        var repo = A.Fake<IProjectRepository>();
        A.CallTo(() => repo.GetProjectsByUserId(1)).Returns(projects);
        var service = CreateService(A.Fake<ICurrentUserService>(), repo, userRepo);

        var result = await service.GetProjectsByUserPuid(user.Puid);

        Assert.Equal(ServiceStatus.Ok200, result.Status);
        Assert.Equal(projects, result.Data);
    }

    // DeleteProject Tests
    [Fact]
    public async Task DeleteProject_EmptyPuid_ReturnsBadRequest()
    {
        var service = CreateService(A.Fake<ICurrentUserService>(), A.Fake<IProjectRepository>(), A.Fake<IUserRepository>());

        var result = await service.DeleteProject("");

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task DeleteProject_NotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IProjectRepository>();
        A.CallTo(() => repo.GetProjectByPuid("puid")).Returns((Project?)null);
        var service = CreateService(A.Fake<ICurrentUserService>(), repo, A.Fake<IUserRepository>());

        var result = await service.DeleteProject("puid");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task DeleteProject_RepoFails_ReturnsInternalServerError()
    {
        var project = CreateProject(id: 1, puid: "puid");
        var repo = A.Fake<IProjectRepository>();
        A.CallTo(() => repo.GetProjectByPuid("puid")).Returns(project);
        A.CallTo(() => repo.DeleteProject(1)).Returns(false);
        var service = CreateService(A.Fake<ICurrentUserService>(), repo, A.Fake<IUserRepository>());

        var result = await service.DeleteProject("puid");

        Assert.Equal(ServiceStatus.InternalServerError500, result.Status);
    }

    [Fact]
    public async Task DeleteProject_Valid_ReturnsNoContent()
    {
        var project = CreateProject(id: 1, puid: "puid");
        var repo = A.Fake<IProjectRepository>();
        A.CallTo(() => repo.GetProjectByPuid("puid")).Returns(project);
        A.CallTo(() => repo.DeleteProject(1)).Returns(true);
        var service = CreateService(A.Fake<ICurrentUserService>(), repo, A.Fake<IUserRepository>());

        var result = await service.DeleteProject("puid");

        Assert.Equal(ServiceStatus.NoContent204, result.Status);
    }

    // ResolveProject Tests
    [Fact]
    public async Task ResolveProject_EmptyUsername_ReturnsBadRequest()
    {
        var service = CreateService(A.Fake<ICurrentUserService>(), A.Fake<IProjectRepository>(), A.Fake<IUserRepository>());

        var result = await service.ResolveProject("", "proj");
        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task ResolveProject_UserNotFound_ReturnsNotFound()
    {
        var userRepo = A.Fake<IUserRepository>();
        A.CallTo(() => userRepo.GetByUsername("user")).Returns((User?)null);
        var service = CreateService(A.Fake<ICurrentUserService>(), A.Fake<IProjectRepository>(), userRepo);

        var result = await service.ResolveProject("user", "project");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task ResolveProject_ProjectNotFound_ReturnsNotFound()
    {
        var user = CreateUser(id: 1);
        var userRepo = A.Fake<IUserRepository>();
        A.CallTo(() => userRepo.GetByUsername("user")).Returns(user);
        var repo = A.Fake<IProjectRepository>();
        A.CallTo(() => repo.GetProjectsByUserId(1)).Returns(new List<Project>());
        var service = CreateService(A.Fake<ICurrentUserService>(), repo, userRepo);

        var result = await service.ResolveProject("user", "project");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task ResolveProject_PublicProject_ReturnsProject()
    {
        var user = CreateUser(id: 1);
        var userRepo = A.Fake<IUserRepository>();
        A.CallTo(() => userRepo.GetByUsername("user")).Returns(user);
        var project = CreateProject(userId: 1, name: "project");
        project.Is_Public = true;
        var repo = A.Fake<IProjectRepository>();
        A.CallTo(() => repo.GetProjectsByUserId(1)).Returns(new List<Project> { project });
        var service = CreateService(A.Fake<ICurrentUserService>(), repo, userRepo);

        var result = await service.ResolveProject("user", "project");

        Assert.Equal(ServiceStatus.Ok200, result.Status);
        Assert.Equal(project, result.Data!.FirstOrDefault());
    }

    [Fact]
    public async Task ResolveProject_AdminUser_ReturnsProject()
    {
        var user = CreateUser(id: 1);
        var userRepo = A.Fake<IUserRepository>();
        A.CallTo(() => userRepo.GetByUsername("owner")).Returns(user);
        var project = CreateProject(userId: 1, name: "project");
        project.Is_Public = false;
        var repo = A.Fake<IProjectRepository>();
        A.CallTo(() => repo.GetProjectsByUserId(1)).Returns(new List<Project> { project });
        var currentUser = A.Fake<ICurrentUserService>();
        A.CallTo(() => currentUser.IsAdmin).Returns(true);
        var service = CreateService(currentUser, repo, userRepo);

        var result = await service.ResolveProject("owner", "project");

        Assert.Equal(ServiceStatus.Ok200, result.Status);
    }

    [Fact]
    public async Task ResolveProject_OwnerUser_ReturnsProject()
    {
        var user = CreateUser(id: 1, puid: "ownerPuid");
        var userRepo = A.Fake<IUserRepository>();
        A.CallTo(() => userRepo.GetByUsername("owner")).Returns(user);
        var project = CreateProject(userId: 1, name: "project");
        project.Is_Public = false;
        var repo = A.Fake<IProjectRepository>();
        A.CallTo(() => repo.GetProjectsByUserId(1)).Returns(new List<Project> { project });
        var currentUser = A.Fake<ICurrentUserService>();
        A.CallTo(() => currentUser.UserPuid).Returns("ownerPuid");
        var service = CreateService(currentUser, repo, userRepo);

        var result = await service.ResolveProject("owner", "project");

        Assert.Equal(ServiceStatus.Ok200, result.Status);
    }

    [Fact]
    public async Task ResolveProject_UnauthorizedUser_ReturnsNotFound()
    {
        var user = CreateUser(id: 1, puid: "ownerPuid");
        var userRepo = A.Fake<IUserRepository>();
        A.CallTo(() => userRepo.GetByUsername("owner")).Returns(user);
        var project = CreateProject(userId: 1, name: "project");
        project.Is_Public = false;
        var repo = A.Fake<IProjectRepository>();
        A.CallTo(() => repo.GetProjectsByUserId(1)).Returns(new List<Project> { project });
        var currentUser = A.Fake<ICurrentUserService>();
        A.CallTo(() => currentUser.UserPuid).Returns("otherUserPuid");
        A.CallTo(() => currentUser.IsAdmin).Returns(false);
        var service = CreateService(currentUser, repo, userRepo);

        var result = await service.ResolveProject("owner", "project");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task ResolveProject_UsernameHasNoPublicProjects_ReturnsNotFound()
    {
        var user = CreateUser(id: 1, puid: "ownerPuid");
        var userRepo = A.Fake<IUserRepository>();
        A.CallTo(() => userRepo.GetByUsername("owner")).Returns(user);
        var project = CreateProject(userId: 1, name: "privateProject");
        project.Is_Public = false;
        var repo = A.Fake<IProjectRepository>();
        A.CallTo(() => repo.GetProjectsByUserId(1)).Returns(new List<Project> { project });
        var currentUser = A.Fake<ICurrentUserService>();
        A.CallTo(() => currentUser.UserPuid).Returns("otherUserPuid");
        A.CallTo(() => currentUser.IsAdmin).Returns(false);
        var service = CreateService(currentUser, repo, userRepo);

        var result = await service.ResolveProject("owner", null);

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task ResolveProject_PublicProjectsOnly_ReturnsProjects()
    {
        var user = CreateUser(id: 1, puid: "ownerPuid");
        var userRepo = A.Fake<IUserRepository>();
        A.CallTo(() => userRepo.GetByUsername("owner")).Returns(user);
        var publicProject = CreateProject(id: 1, userId: 1, name: "publicProject");
        publicProject.Is_Public = true;
        var privateProject = CreateProject(id: 2, userId: 1, name: "privateProject");
        privateProject.Is_Public = false;
        var repo = A.Fake<IProjectRepository>();
        A.CallTo(() => repo.GetProjectsByUserId(1)).Returns(new List<Project> { publicProject, privateProject });
        var currentUser = A.Fake<ICurrentUserService>();
        A.CallTo(() => currentUser.UserPuid).Returns("otherUserPuid");
        A.CallTo(() => currentUser.IsAdmin).Returns(false);
        var service = CreateService(currentUser, repo, userRepo);

        var result = await service.ResolveProject("owner", null);

        Assert.Equal(ServiceStatus.Ok200, result.Status);
        Assert.Single(result.Data!);
        Assert.Equal("publicProject", result.Data![0].Name);
    }

    [Fact]
    public async Task ResolveProject_OwnerUser_ReturnsProjects()
    {
        var user = CreateUser(id: 1, puid: "ownerPuid");
        var userRepo = A.Fake<IUserRepository>();
        A.CallTo(() => userRepo.GetByUsername("owner")).Returns(user);
        var project1 = CreateProject(id: 1, userId: 1, name: "project1");
        var project2 = CreateProject(id: 2, userId: 1, name: "project2");
        var repo = A.Fake<IProjectRepository>();
        A.CallTo(() => repo.GetProjectsByUserId(1)).Returns(new List<Project> { project1, project2 });
        var currentUser = A.Fake<ICurrentUserService>();
        A.CallTo(() => currentUser.UserPuid).Returns("ownerPuid");
        var service = CreateService(currentUser, repo, userRepo);

        var result = await service.ResolveProject("owner", null);

        Assert.Equal(ServiceStatus.Ok200, result.Status);
        Assert.Equal(2, result.Data!.Count);
    }
}
