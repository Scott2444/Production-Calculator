using System.Diagnostics.CodeAnalysis;
using FakeItEasy;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Services;

namespace ProductionCalculator.Business.Tests;

[ExcludeFromCodeCoverage]
public class MachineServiceTests
{
    private static Project CreateProject(int id = 1, string puid = "project123")
    {
        return new Project
        {
            Project_Id = id,
            User_Id = 1,
            Puid = puid,
            Name = "Test Project",
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    private static Machine CreateMachine(int id = 1, int projectId = 1, string puid = "mach123", string name = "Machine")
    {
        return new Machine
        {
            Machine_Id = id,
            Project_Id = projectId,
            Puid = puid,
            Name = name,
            Description = "Description",
            Base_Speed = 10.0,
            Version = 1,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    private static Recipe CreateRecipe(int id = 1, int projectId = 1, string puid = "recipe123", string name = "Recipe")
    {
        return new Recipe
        {
            Recipe_Id = id,
            Project_Id = projectId,
            Puid = puid,
            Name = name,
            Base_Crafting_Time = 1.0,
            Version = 1,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    private static MachineService CreateService(
        IMachineRepository repo, 
        IMachineRecipeRepository machineRecipeRepo, 
        IMachineAttributeRepository machineAttributeRepo,
        IRecipeRepository recipeRepo, 
        IAttributeRepository attributeRepo,
        IProjectRepository projectRepo)
    {
        var currentUser = A.Fake<ICurrentUserService>();
        return new MachineService(currentUser, repo, machineRecipeRepo, machineAttributeRepo, recipeRepo, attributeRepo, projectRepo);
    }

    private static MachineService CreateService(
        IMachineRepository repo,
        IMachineRecipeRepository machineRecipeRepo,
        IRecipeRepository recipeRepo,
        IProjectRepository projectRepo)
    {
        return CreateService(repo, machineRecipeRepo, A.Fake<IMachineAttributeRepository>(), recipeRepo, A.Fake<IAttributeRepository>(), projectRepo);
    }

    [Fact]
    public async Task AddMachine_EmptyName_ReturnsBadRequest()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, A.Fake<IMachineAttributeRepository>(), recipeRepo, A.Fake<IAttributeRepository>(), projectRepo);

        var result = await service.AddMachine("projPuid", "", "desc", 10.0, new List<string>());

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task AddMachine_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, A.Fake<IMachineAttributeRepository>(), recipeRepo, A.Fake<IAttributeRepository>(), projectRepo);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.AddMachine("missing", "Mach", "desc", 10.0, new List<string>());

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task AddMachine_DuplicateNameInProject_ReturnsConflict()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, A.Fake<IMachineAttributeRepository>(), recipeRepo, A.Fake<IAttributeRepository>(), projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetMachinesByProjectId(10)).Returns(new List<Machine> { CreateMachine(name: "Existing") });

        var result = await service.AddMachine("projPuid", "Existing", "desc", 10.0, new List<string>());

        Assert.Equal(ServiceStatus.Conflict409, result.Status);
    }

    [Fact]
    public async Task AddMachine_InvalidBaseSpeed_ReturnsBadRequest()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, A.Fake<IMachineAttributeRepository>(), recipeRepo, A.Fake<IAttributeRepository>(), projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetMachinesByProjectId(10)).Returns(new List<Machine>());

        var result = await service.AddMachine("projPuid", "Mach", "desc", 0, new List<string>());

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task AddMachine_RecipeNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, A.Fake<IMachineAttributeRepository>(), recipeRepo, A.Fake<IAttributeRepository>(), projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetMachinesByProjectId(10)).Returns(new List<Machine>());
        A.CallTo(() => recipeRepo.GetByPuid("missingRecipe")).Returns(Task.FromResult<Recipe?>(null));

        var result = await service.AddMachine("projPuid", "Mach", "desc", 10.0, new List<string> { "missingRecipe" });

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task AddMachine_ValidRequest_ReturnsCreatedAndSavesToRepo()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var machineAttributeRepo = A.Fake<IMachineAttributeRepository>();
        var attributeRepo = A.Fake<IAttributeRepository>();
        var service = CreateService(repo, machineRecipeRepo, machineAttributeRepo, recipeRepo, attributeRepo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        var recipe = CreateRecipe(id: 20, projectId: 10, puid: "recPuid");
        var attribute = new ProjectAttribute
        {
            Attribute_Id = 15,
            Project_Id = 10,
            Puid = "a1",
            Name = "Attr",
            Description = "desc",
            Unit = "u",
            Version = 1,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetMachinesByProjectId(10)).Returns(new List<Machine>());
        A.CallTo(() => recipeRepo.GetByPuid("recPuid")).Returns(recipe);
        A.CallTo(() => attributeRepo.GetAttributeByPuid("a1")).Returns(attribute);

        var result = await service.AddMachine("projPuid", "NewMach", "desc", 10.0, new List<string> { "recPuid" }, [new AttributeRateRequest { Puid = "a1", Rate = 2 }]);

        Assert.True(result.Success);
        Assert.Equal(ServiceStatus.Created201, result.Status);
        A.CallTo(() => repo.AddMachine(A<Machine>.That.Matches(m => m.Name == "NewMach" && m.Project_Id == 10))).MustHaveHappenedOnceExactly();
        A.CallTo(() => machineRecipeRepo.AddMachineRecipes(A<IEnumerable<MachineRecipe>>.That.Matches(mrs => mrs.Any(mr => mr.Recipe_Id == 20)))).MustHaveHappenedOnceExactly();
        A.CallTo(() => machineAttributeRepo.AddMachineAttributes(A<IEnumerable<MachineAttribute>>.That.Matches(mas => mas.Any(ma => ma.Attribute_Id == 15 && ma.Rate == 2)))).MustHaveHappenedOnceExactly();
        A.CallTo(() => projectRepo.UpdateProject(A<Project>.That.Matches(p => p.Project_Id == 10))).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetMachineByPuid_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, A.Fake<IMachineAttributeRepository>(), recipeRepo, A.Fake<IAttributeRepository>(), projectRepo);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.GetMachineByPuid("missing", "machPuid");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetMachineByPuid_MachineNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, recipeRepo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetMachineByPuid("missing")).Returns(Task.FromResult<Machine?>(null));

        var result = await service.GetMachineByPuid("projPuid", "missing");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetMachineByPuid_MachineBelongsToDifferentProject_ReturnsNotFound()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, recipeRepo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        var machine = CreateMachine(projectId: 11, puid: "machPuid"); // Different project
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetMachineByPuid("machPuid")).Returns(machine);

        var result = await service.GetMachineByPuid("projPuid", "machPuid");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetMachineByPuid_ValidInputs_ReturnsMachine()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, recipeRepo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        var machine = CreateMachine(id: 5, projectId: 10, puid: "machPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetMachineByPuid("machPuid")).Returns(machine);
        A.CallTo(() => machineRecipeRepo.GetByMachineId(5)).Returns(new List<MachineRecipe>());

        var result = await service.GetMachineByPuid("projPuid", "machPuid");

        Assert.True(result.Success);
        Assert.Equal("machPuid", result.Data!.Puid);
    }

    [Fact]
    public async Task GetMachinesByProjectPuid_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, recipeRepo, projectRepo);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.GetMachinesByProjectPuid("missing");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetMachinesByProjectPuid_ProjectExists_ReturnsMachineList()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, recipeRepo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        var machines = new List<Machine> { CreateMachine(id: 1, projectId: 10), CreateMachine(id: 2, projectId: 10) };
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetMachinesByProjectId(10)).Returns(machines);

        var result = await service.GetMachinesByProjectPuid("projPuid");

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Count);
    }

    [Fact]
    public async Task UpdateMachine_EmptyName_ReturnsBadRequest()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, recipeRepo, projectRepo);

        var result = await service.UpdateMachine("projPuid", "machPuid", "", "desc", 10.0, new List<string>());

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task UpdateMachine_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, recipeRepo, projectRepo);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.UpdateMachine("missing", "machPuid", "Name", "desc", 10.0, new List<string>());

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateMachine_MachineNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, recipeRepo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetMachineByPuid("missing")).Returns(Task.FromResult<Machine?>(null));

        var result = await service.UpdateMachine("projPuid", "missing", "Name", "desc", 10.0, new List<string>());

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateMachine_MachineBelongsToDifferentProject_ReturnsNotFound()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, recipeRepo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        var machine = CreateMachine(projectId: 11, puid: "machPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetMachineByPuid("machPuid")).Returns(machine);

        var result = await service.UpdateMachine("projPuid", "machPuid", "Name", "desc", 10.0, new List<string>());

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateMachine_DuplicateNameOtherThanSelf_ReturnsConflict()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, recipeRepo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        var machine = CreateMachine(id: 1, projectId: 10, puid: "mach1", name: "OldName");
        var otherMachine = CreateMachine(id: 2, projectId: 10, puid: "mach2", name: "DuplicateName");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetMachineByPuid("mach1")).Returns(machine);
        A.CallTo(() => repo.GetMachinesByProjectId(10)).Returns(new List<Machine> { machine, otherMachine });

        var result = await service.UpdateMachine("projPuid", "mach1", "DuplicateName", "desc", 10.0, new List<string>());

        Assert.Equal(ServiceStatus.Conflict409, result.Status);
    }

    [Fact]
    public async Task UpdateMachine_InvalidBaseSpeed_ReturnsBadRequest()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, recipeRepo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        var machine = CreateMachine(id: 1, projectId: 10, puid: "mach1");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetMachineByPuid("mach1")).Returns(machine);
        A.CallTo(() => repo.GetMachinesByProjectId(10)).Returns(new List<Machine> { machine });

        var result = await service.UpdateMachine("projPuid", "mach1", "Name", "desc", 0, new List<string>());

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task UpdateMachine_RecipeNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, recipeRepo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        var machine = CreateMachine(id: 1, projectId: 10, puid: "mach1");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetMachineByPuid("mach1")).Returns(machine);
        A.CallTo(() => repo.GetMachinesByProjectId(10)).Returns(new List<Machine> { machine });
        A.CallTo(() => recipeRepo.GetByPuid("missing")).Returns(Task.FromResult<Recipe?>(null));

        var result = await service.UpdateMachine("projPuid", "mach1", "Name", "desc", 10.0, new List<string> { "missing" });

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task UpdateMachine_ValidRequest_ReturnsSuccessAndUpdatesRepo()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, recipeRepo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        var machine = CreateMachine(id: 1, projectId: 10, puid: "mach1");
        var recipe = CreateRecipe(id: 20, projectId: 10, puid: "rec1");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetMachineByPuid("mach1")).Returns(machine);
        A.CallTo(() => repo.GetMachinesByProjectId(10)).Returns(new List<Machine> { machine });
        A.CallTo(() => recipeRepo.GetByPuid("rec1")).Returns(recipe);
        A.CallTo(() => machineRecipeRepo.GetByMachineId(1)).Returns(new List<MachineRecipe>());

        var result = await service.UpdateMachine("projPuid", "mach1", "NewName", "NewDesc", 15.0, new List<string> { "rec1" });

        Assert.True(result.Success);
        A.CallTo(() => repo.UpdateMachine(A<Machine>.That.Matches(m => m.Name == "NewName" && m.Base_Speed == 15.0))).MustHaveHappenedOnceExactly();
        A.CallTo(() => machineRecipeRepo.AddMachineRecipes(A<IEnumerable<MachineRecipe>>.That.Matches(mrs => mrs.Any(mr => mr.Recipe_Id == 20)))).MustHaveHappenedOnceExactly();
        A.CallTo(() => projectRepo.UpdateProject(A<Project>.That.Matches(p => p.Project_Id == 10))).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteMachine_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, recipeRepo, projectRepo);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.DeleteMachine("missing", "machPuid");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task DeleteMachine_MachineNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, recipeRepo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetMachineByPuid("missing")).Returns(Task.FromResult<Machine?>(null));

        var result = await service.DeleteMachine("projPuid", "missing");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task DeleteMachine_RepoReturnsFalse_ReturnsInternalServerError()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, recipeRepo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        var machine = CreateMachine(id: 1, projectId: 10, puid: "mach1");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetMachineByPuid("mach1")).Returns(machine);
        A.CallTo(() => repo.DeleteMachine(1)).Returns(false);

        var result = await service.DeleteMachine("projPuid", "mach1");

        Assert.Equal(ServiceStatus.InternalServerError500, result.Status);
    }

    [Fact]
    public async Task DeleteMachine_ValidRequest_ReturnsNoContent()
    {
        var repo = A.Fake<IMachineRepository>();
        var machineRecipeRepo = A.Fake<IMachineRecipeRepository>();
        var recipeRepo = A.Fake<IRecipeRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var service = CreateService(repo, machineRecipeRepo, recipeRepo, projectRepo);
        var project = CreateProject(id: 10, puid: "projPuid");
        var machine = CreateMachine(id: 1, projectId: 10, puid: "mach1");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetMachineByPuid("mach1")).Returns(machine);
        A.CallTo(() => repo.DeleteMachine(1)).Returns(true);

        var result = await service.DeleteMachine("projPuid", "mach1");

        Assert.Equal(ServiceStatus.NoContent204, result.Status);
        A.CallTo(() => projectRepo.UpdateProject(A<Project>.That.Matches(p => p.Project_Id == 10))).MustHaveHappenedOnceExactly();
    }
}
