using FakeItEasy;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Services;

namespace ProductionCalculator.Business.Tests;

public class AttributeServiceTests
{
    private static Project CreateProject(int id = 1, string puid = "project123", string? aliasPuid = null)
    {
        return new Project
        {
            Project_Id = id,
            User_Id = 1,
            Puid = puid,
            Name = "Test Project",
            Alias_Project_Puid = aliasPuid,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    private static ProjectAttribute CreateAttribute(int id = 1, int projectId = 1, string puid = "attr123", string name = "Attribute")
    {
        return new ProjectAttribute
        {
            Attribute_Id = id,
            Project_Id = projectId,
            Puid = puid,
            Name = name,
            Description = "Description",
            Unit = "u",
            Version = 1,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    private static AttributeService CreateService(IAttributeRepository repo, IProjectRepository projectRepo)
    {
        var currentUser = A.Fake<ICurrentUserService>();
        return new AttributeService(currentUser, repo, projectRepo);
    }

    [Fact]
    public async Task AddAttribute_ValidRequest_ReturnsCreatedAndSavesToRepo()
    {
        var repo = A.Fake<IAttributeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetAttributesByProjectId(10)).Returns(new List<ProjectAttribute>());
        A.CallTo(() => repo.PuidExists(A<string>._)).Returns(false);

        var result = await service.AddAttribute("projPuid", "NewAttr", "desc", "m/s");

        Assert.True(result.Success);
        Assert.Equal(ServiceStatus.Created201, result.Status);
        A.CallTo(() => repo.AddAttribute(A<ProjectAttribute>.That.Matches(p => p.Name == "NewAttr" && p.Project_Id == 10))).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetAttributeByPuid_AliasedProject_Redirects()
    {
        var repo = A.Fake<IAttributeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(puid: "alias", aliasPuid: "canonical");
        A.CallTo(() => projectRepo.GetProjectByPuid("alias")).Returns(project);

        var result = await service.GetAttributeByPuid("alias", "attrPuid");

        Assert.Equal(ServiceStatus.SeeOther303, result.Status);
        Assert.Equal("/api/projects/canonical/attributes/attrPuid", result.Location);
    }

    [Fact]
    public async Task GetAttributesByProjectPuid_ProjectExists_ReturnsList()
    {
        var repo = A.Fake<IAttributeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 1, puid: "proj");
        var attributes = new List<ProjectAttribute> { CreateAttribute(id: 1, projectId: 1), CreateAttribute(id: 2, projectId: 1) };
        A.CallTo(() => projectRepo.GetProjectByPuid("proj")).Returns(project);
        A.CallTo(() => repo.GetAttributesByProjectId(1)).Returns(attributes);

        var result = await service.GetAttributesByProjectPuid("proj");

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Count);
    }

    [Fact]
    public async Task UpdateAttribute_DuplicateName_ReturnsConflict()
    {
        var repo = A.Fake<IAttributeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 1, puid: "proj");
        var attribute = CreateAttribute(id: 10, projectId: 1, puid: "attrPuid", name: "Original");
        var other = CreateAttribute(id: 11, projectId: 1, puid: "other", name: "Dupe");
        A.CallTo(() => projectRepo.GetProjectByPuid("proj")).Returns(project);
        A.CallTo(() => repo.GetAttributeByPuid("attrPuid")).Returns(attribute);
        A.CallTo(() => repo.GetAttributesByProjectId(1)).Returns(new List<ProjectAttribute> { attribute, other });

        var result = await service.UpdateAttribute("proj", "attrPuid", "Dupe", "desc", "u");

        Assert.Equal(ServiceStatus.Conflict409, result.Status);
    }

    [Fact]
    public async Task DeleteAttribute_ValidRequest_ReturnsNoContent()
    {
        var repo = A.Fake<IAttributeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 1, puid: "proj");
        var attribute = CreateAttribute(id: 10, projectId: 1, puid: "attrPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("proj")).Returns(project);
        A.CallTo(() => repo.GetAttributeByPuid("attrPuid")).Returns(attribute);
        A.CallTo(() => repo.DeleteAttribute(10)).Returns(true);

        var result = await service.DeleteAttribute("proj", "attrPuid");

        Assert.Equal(ServiceStatus.NoContent204, result.Status);
    }
}
