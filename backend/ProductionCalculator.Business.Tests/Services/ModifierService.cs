using FakeItEasy;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Services;

namespace ProductionCalculator.Business.Tests;

public class ModifierServiceTests
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

    private static Modifier CreateModifier(int id = 1, int projectId = 1, string puid = "mod123", string name = "Modifier")
    {
        return new Modifier
        {
            Modifier_Id = id,
            Project_Id = projectId,
            Puid = puid,
            Name = name,
            Description = "Description",
            Flat_Bonus = 1.0,
            Percent_Bonus = 0.5,
            Multiplicative_Bonus = 1.1,
            Input_Percent = 1.0,
            Output_Percent = 1.0,
            Version = 1,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    private static ModifierService CreateService(IModifierRepository repo, IProjectRepository projectRepo)
    {
        var currentUser = A.Fake<ICurrentUserService>();
        return new ModifierService(currentUser, projectRepo, repo, A.Fake<IModifierAttributeRepository>(), A.Fake<IAttributeRepository>());
    }

    [Fact]
    public async Task AddModifier_EmptyName_ReturnsBadRequest()
    {
        var repo = A.Fake<IModifierRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);

        var result = await service.AddModifier("projectPuid", "", "desc", 0, 0, 0);

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task AddModifier_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IModifierRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.AddModifier("missing", "Mod", "desc", 0, 0, 0);

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task AddModifier_DuplicateNameInProject_ReturnsConflict()
    {
        var repo = A.Fake<IModifierRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetModifiersByProjectId(10)).Returns(new List<Modifier> { CreateModifier(name: "Existing") });

        var result = await service.AddModifier("projPuid", "Existing", "desc", 0, 0, 0);

        Assert.Equal(ServiceStatus.Conflict409, result.Status);
    }

    [Fact]
    public async Task AddModifier_ValidRequest_ReturnsCreatedAndSavesToRepo()
    {
        var repo = A.Fake<IModifierRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetModifiersByProjectId(10)).Returns(new List<Modifier>());
        A.CallTo(() => repo.PuidExists(A<string>._)).Returns(false);

        var result = await service.AddModifier("projPuid", "NewMod", "desc", 1.0, 2.0, 3.0);

        Assert.True(result.Success);
        Assert.Equal(ServiceStatus.Created201, result.Status);
        A.CallTo(() => repo.AddModifier(A<Modifier>.That.Matches(m => m.Name == "NewMod" && m.Project_Id == 10))).MustHaveHappenedOnceExactly();
        A.CallTo(() => projectRepo.UpdateProject(A<Project>.That.Matches(p => p.Project_Id == 10))).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetModifierByPuid_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IModifierRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.GetModifierByPuid("missing", "modPuid");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetModifierByPuid_AliasedProject_RedirectsToCanonical()
    {
        var repo = A.Fake<IModifierRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(puid: "alias", aliasPuid: "canonical");
        A.CallTo(() => projectRepo.GetProjectByPuid("alias")).Returns(project);

        var result = await service.GetModifierByPuid("alias", "modPuid");

        Assert.Equal(ServiceStatus.SeeOther303, result.Status);
        Assert.Equal("/api/projects/canonical/modifiers/modPuid", result.Location);
    }

    [Fact]
    public async Task GetModifierByPuid_ModifierNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IModifierRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetModifierByPuid("missing")).Returns(Task.FromResult<Modifier?>(null));

        var result = await service.GetModifierByPuid("projPuid", "missing");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetModifierByPuid_ModifierBelongsToDifferentProject_ReturnsNotFound()
    {
        var repo = A.Fake<IModifierRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        var modifier = CreateModifier(projectId: 99, puid: "otherMod");
        A.CallTo(() => repo.GetModifierByPuid("otherMod")).Returns(modifier);

        var result = await service.GetModifierByPuid("projPuid", "otherMod");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetModifierByPuid_ValidInputs_ReturnsModifier()
    {
        var repo = A.Fake<IModifierRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        var modifier = CreateModifier(projectId: 10, puid: "modPuid");
        A.CallTo(() => repo.GetModifierByPuid("modPuid")).Returns(modifier);

        var result = await service.GetModifierByPuid("projPuid", "modPuid");

        Assert.True(result.Success);
        Assert.Equal(modifier, result.Data);
    }

    [Fact]
    public async Task GetModifiersByProjectPuid_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IModifierRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.GetModifiersByProjectPuid("missing");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetModifiersByProjectPuid_AliasedProject_RedirectsToCanonical()
    {
        var repo = A.Fake<IModifierRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(puid: "alias", aliasPuid: "canonical");
        A.CallTo(() => projectRepo.GetProjectByPuid("alias")).Returns(project);

        var result = await service.GetModifiersByProjectPuid("alias");

        Assert.Equal(ServiceStatus.SeeOther303, result.Status);
        Assert.Equal("/api/projects/canonical/modifiers", result.Location);
    }

    [Fact]
    public async Task GetModifiersByProjectPuid_ProjectExists_ReturnsModifierList()
    {
        var repo = A.Fake<IModifierRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        var modifiers = new List<Modifier> { CreateModifier(projectId: 10) };
        A.CallTo(() => repo.GetModifiersByProjectId(10)).Returns(modifiers);

        var result = await service.GetModifiersByProjectPuid("projPuid");

        Assert.True(result.Success);
        Assert.Equal(modifiers, result.Data);
    }

    [Fact]
    public async Task UpdateModifier_EmptyName_ReturnsBadRequest()
    {
        var repo = A.Fake<IModifierRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);

        var result = await service.UpdateModifier("projPuid", "modPuid", "", "desc", 0, 0, 0);

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task UpdateModifier_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IModifierRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.UpdateModifier("missing", "modPuid", "Name", "desc", 0, 0, 0);

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateModifier_ModifierNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IModifierRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetModifierByPuid("missing")).Returns(Task.FromResult<Modifier?>(null));

        var result = await service.UpdateModifier("projPuid", "missing", "Name", "desc", 0, 0, 0);

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateModifier_ModifierBelongsToDifferentProject_ReturnsNotFound()
    {
        var repo = A.Fake<IModifierRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        var modifier = CreateModifier(projectId: 99, puid: "otherMod");
        A.CallTo(() => repo.GetModifierByPuid("otherMod")).Returns(modifier);

        var result = await service.UpdateModifier("projPuid", "otherMod", "Name", "desc", 0, 0, 0);

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateModifier_DuplicateNameOtherThanSelf_ReturnsConflict()
    {
        var repo = A.Fake<IModifierRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        var modifier = CreateModifier(projectId: 10, puid: "modPuid", name: "MyMod");
        A.CallTo(() => repo.GetModifierByPuid("modPuid")).Returns(modifier);
        A.CallTo(() => repo.GetModifiersByProjectId(10)).Returns(new List<Modifier> { CreateModifier(name: "Existing", puid: "other") });

        var result = await service.UpdateModifier("projPuid", "modPuid", "Existing", "desc", 0, 0, 0);

        Assert.Equal(ServiceStatus.Conflict409, result.Status);
    }

    [Fact]
    public async Task UpdateModifier_ValidRequest_ReturnsSuccessAndUpdatesRepo()
    {
        var repo = A.Fake<IModifierRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        var modifier = CreateModifier(projectId: 10, puid: "modPuid", name: "OldName");
        A.CallTo(() => repo.GetModifierByPuid("modPuid")).Returns(modifier);
        A.CallTo(() => repo.GetModifiersByProjectId(10)).Returns(new List<Modifier> { modifier });

        var result = await service.UpdateModifier("projPuid", "modPuid", "NewName", "new desc", 10.0, 20.0, 30.0);

        Assert.True(result.Success);
        Assert.Equal(ServiceStatus.Ok200, result.Status);
        A.CallTo(() => repo.UpdateModifier(A<Modifier>.That.Matches(m => m.Name == "NewName" && m.Version == 2))).MustHaveHappenedOnceExactly();
        A.CallTo(() => projectRepo.UpdateProject(A<Project>.That.Matches(p => p.Project_Id == 10))).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteModifier_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IModifierRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.DeleteModifier("missing", "modPuid");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task DeleteModifier_ModifierNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IModifierRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetModifierByPuid("missing")).Returns(Task.FromResult<Modifier?>(null));

        var result = await service.DeleteModifier("projPuid", "missing");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task DeleteModifier_RepoReturnsFalse_ReturnsInternalServerError()
    {
        var repo = A.Fake<IModifierRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        var modifier = CreateModifier(projectId: 10, puid: "modPuid");
        A.CallTo(() => repo.GetModifierByPuid("modPuid")).Returns(modifier);
        A.CallTo(() => repo.DeleteModifier(modifier.Modifier_Id)).Returns(false);

        var result = await service.DeleteModifier("projPuid", "modPuid");

        Assert.Equal(ServiceStatus.InternalServerError500, result.Status);
    }

    [Fact]
    public async Task DeleteModifier_ValidRequest_ReturnsNoContent()
    {
        var repo = A.Fake<IModifierRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        var modifier = CreateModifier(projectId: 10, puid: "modPuid");
        A.CallTo(() => repo.GetModifierByPuid("modPuid")).Returns(modifier);
        A.CallTo(() => repo.DeleteModifier(modifier.Modifier_Id)).Returns(true);

        var result = await service.DeleteModifier("projPuid", "modPuid");

        Assert.Equal(ServiceStatus.NoContent204, result.Status);
        A.CallTo(() => projectRepo.UpdateProject(A<Project>.That.Matches(p => p.Project_Id == 10))).MustHaveHappenedOnceExactly();
    }
}
