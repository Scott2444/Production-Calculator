using FakeItEasy;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Services;

namespace ProductionCalculator.Business.Tests;

public class WorkflowServiceTests
{
    private static Project CreateProject(int id = 1, string puid = "project1234")
    {
        return new Project
        {
            Project_Id = id,
            User_Id = 1,
            Puid = puid,
            Name = "Project",
            Description = null,
            Is_Public = false,
            Alias_Project_Puid = null,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    private static Workflow CreateWorkflow(int id = 1, int projectId = 1, string puid = "workflowPuid", string name = "Workflow")
    {
        return new Workflow
        {
            Workflow_Id = id,
            Project_Id = projectId,
            Puid = puid,
            Name = name,
            Description = "desc",
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    private static WorkflowService CreateService(
        IWorkflowRepository repo,
        IProjectRepository projectRepo,
        IWorkflowNodeService workflowNodeService)
    {
        var currentUser = A.Fake<ICurrentUserService>();
        return new WorkflowService(currentUser, repo, projectRepo, workflowNodeService);
    }

    [Fact]
    public async Task AddWorkflow_EmptyName_ReturnsBadRequest()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);

        var result = await service.AddWorkflow("project", "", null);

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task AddWorkflow_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);

        A.CallTo(() => projectRepo.GetProjectByPuid("project")).Returns(Task.FromResult<Project?>(null));

        var result = await service.AddWorkflow("project", "wf", null);

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task AddWorkflow_DuplicateNameInProject_ReturnsConflict()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);

        var project = CreateProject(id: 7, puid: "project");
        A.CallTo(() => projectRepo.GetProjectByPuid("project")).Returns(project);
        A.CallTo(() => repo.GetWorkflowsByProjectId(7)).Returns(new List<Workflow> { CreateWorkflow(id: 1, projectId: 7, name: "wf") });

        var result = await service.AddWorkflow("project", "wf", null);

        Assert.Equal(ServiceStatus.Conflict409, result.Status);
    }

    [Fact]
    public async Task AddWorkflow_ValidRequest_ReturnsCreatedAndSavesToRepo()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);

        var project = CreateProject(id: 7, puid: "project");
        A.CallTo(() => projectRepo.GetProjectByPuid("project")).Returns(project);
        A.CallTo(() => repo.GetWorkflowsByProjectId(7)).Returns(new List<Workflow>());
        A.CallTo(() => repo.PuidExists(A<string>._)).Returns(Task.FromResult(false));

        var result = await service.AddWorkflow("project", "wf", "d");

        Assert.True(result.Success);
        Assert.Equal(ServiceStatus.Created201, result.Status);
        Assert.NotNull(result.Data);
        Assert.Equal(7, result.Data!.Project_Id);
        Assert.Equal("wf", result.Data.Name);
        Assert.False(string.IsNullOrWhiteSpace(result.Data.Puid));
        Assert.Equal(10, result.Data.Puid.Length);

        A.CallTo(() => repo.AddWorkflow(A<Workflow>.That.Matches(w => w.Project_Id == 7 && w.Name == "wf" && w.Puid.Length == 10)))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UpdateWorkflow_EmptyName_ReturnsBadRequest()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);

        var result = await service.UpdateWorkflow("project", "wfPuid", "", null);

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task UpdateWorkflow_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);

        A.CallTo(() => projectRepo.GetProjectByPuid("project")).Returns(Task.FromResult<Project?>(null));

        var result = await service.UpdateWorkflow("project", "wfPuid", "new", null);

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateWorkflow_WorkflowNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);

        var project = CreateProject(id: 7, puid: "project");
        A.CallTo(() => projectRepo.GetProjectByPuid("project")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("wfPuid")).Returns(Task.FromResult<Workflow?>(null));

        var result = await service.UpdateWorkflow("project", "wfPuid", "new", null);

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateWorkflow_WorkflowBelongsToDifferentProject_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);

        var project = CreateProject(id: 7, puid: "project");
        A.CallTo(() => projectRepo.GetProjectByPuid("project")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("wfPuid")).Returns(CreateWorkflow(id: 1, projectId: 999, puid: "wfPuid"));

        var result = await service.UpdateWorkflow("project", "wfPuid", "new", null);

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateWorkflow_DuplicateNameOtherThanSelf_ReturnsConflict()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);

        var project = CreateProject(id: 7, puid: "project");
        var workflow = CreateWorkflow(id: 10, projectId: 7, puid: "wfPuid", name: "old");
        A.CallTo(() => projectRepo.GetProjectByPuid("project")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("wfPuid")).Returns(workflow);
        A.CallTo(() => repo.GetWorkflowsByProjectId(7)).Returns(new List<Workflow>
        {
            workflow,
            CreateWorkflow(id: 11, projectId: 7, puid: "other", name: "new")
        });

        var result = await service.UpdateWorkflow("project", "wfPuid", "new", null);

        Assert.Equal(ServiceStatus.Conflict409, result.Status);
    }

    [Fact]
    public async Task UpdateWorkflow_ValidRequest_ReturnsSuccessAndUpdatesRepo()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);

        var project = CreateProject(id: 7, puid: "project");
        var workflow = CreateWorkflow(id: 10, projectId: 7, puid: "wfPuid", name: "old");
        A.CallTo(() => projectRepo.GetProjectByPuid("project")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("wfPuid")).Returns(workflow);
        A.CallTo(() => repo.GetWorkflowsByProjectId(7)).Returns(new List<Workflow> { workflow });

        var result = await service.UpdateWorkflow("project", "wfPuid", "new", "d");

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("new", result.Data!.Name);

        A.CallTo(() => repo.UpdateWorkflow(A<Workflow>.That.Matches(w => w.Workflow_Id == 10 && w.Name == "new")))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetWorkflowByPuid_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);
        A.CallTo(() => projectRepo.GetProjectByPuid("project")).Returns(Task.FromResult<Project?>(null));

        var result = await service.GetWorkflowByPuid("project", "wf");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetWorkflowByPuid_WorkflowNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);
        var project = CreateProject(id: 7, puid: "project");

        A.CallTo(() => projectRepo.GetProjectByPuid("project")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("wf")).Returns(Task.FromResult<Workflow?>(null));

        var result = await service.GetWorkflowByPuid("project", "wf");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetWorkflowByPuid_WorkflowBelongsToDifferentProject_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);
        var project = CreateProject(id: 7, puid: "project");

        A.CallTo(() => projectRepo.GetProjectByPuid("project")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("wf")).Returns(CreateWorkflow(id: 1, projectId: 999, puid: "wf"));

        var result = await service.GetWorkflowByPuid("project", "wf");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetWorkflowByPuid_ValidInputs_ReturnsWorkflow()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);
        var project = CreateProject(id: 7, puid: "project");
        var workflow = CreateWorkflow(id: 1, projectId: 7, puid: "wf");

        A.CallTo(() => projectRepo.GetProjectByPuid("project")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("wf")).Returns(workflow);

        var result = await service.GetWorkflowByPuid("project", "wf");

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("wf", result.Data!.Puid);
    }

    [Fact]
    public async Task GetWorkflowsByProjectPuid_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);

        A.CallTo(() => projectRepo.GetProjectByPuid("project")).Returns(Task.FromResult<Project?>(null));

        var result = await service.GetWorkflowsByProjectPuid("project");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetWorkflowsByProjectPuid_ProjectExists_ReturnsWorkflowList()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);
        var project = CreateProject(id: 7, puid: "project");
        var workflows = new List<Workflow> { CreateWorkflow(id: 1, projectId: 7), CreateWorkflow(id: 2, projectId: 7) };

        A.CallTo(() => projectRepo.GetProjectByPuid("project")).Returns(project);
        A.CallTo(() => repo.GetWorkflowsByProjectId(7)).Returns(workflows);

        var result = await service.GetWorkflowsByProjectPuid("project");

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data!.Count);
    }

    [Fact]
    public async Task DeleteWorkflow_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);
        A.CallTo(() => projectRepo.GetProjectByPuid("project")).Returns(Task.FromResult<Project?>(null));

        var result = await service.DeleteWorkflow("project", "wf");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task DeleteWorkflow_WorkflowNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);
        var project = CreateProject(id: 7, puid: "project");

        A.CallTo(() => projectRepo.GetProjectByPuid("project")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("wf")).Returns(Task.FromResult<Workflow?>(null));

        var result = await service.DeleteWorkflow("project", "wf");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task DeleteWorkflow_RepoReturnsFalse_ReturnsInternalServerError()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);
        var project = CreateProject(id: 7, puid: "project");
        var workflow = CreateWorkflow(id: 3, projectId: 7, puid: "wf");

        A.CallTo(() => projectRepo.GetProjectByPuid("project")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("wf")).Returns(workflow);
        A.CallTo(() => repo.DeleteWorkflow(3)).Returns(Task.FromResult(false));

        var result = await service.DeleteWorkflow("project", "wf");

        Assert.Equal(ServiceStatus.InternalServerError500, result.Status);
    }

    [Fact]
    public async Task DeleteWorkflow_ValidRequest_ReturnsNoContent()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);
        var project = CreateProject(id: 7, puid: "project");
        var workflow = CreateWorkflow(id: 3, projectId: 7, puid: "wf");

        A.CallTo(() => projectRepo.GetProjectByPuid("project")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("wf")).Returns(workflow);
        A.CallTo(() => repo.DeleteWorkflow(3)).Returns(Task.FromResult(true));

        var result = await service.DeleteWorkflow("project", "wf");

        Assert.True(result.Success);
        Assert.Equal(ServiceStatus.NoContent204, result.Status);
    }

    [Fact]
    public async Task UpdateTargetDemand_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);
        A.CallTo(() => projectRepo.GetProjectByPuid("project")).Returns(Task.FromResult<Project?>(null));

        var result = await service.UpdateTargetDemand("project", "wf", new List<(string, double)>());

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateTargetDemand_WorkflowNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);
        var project = CreateProject(id: 7, puid: "project");

        A.CallTo(() => projectRepo.GetProjectByPuid("project")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("wf")).Returns(Task.FromResult<Workflow?>(null));

        var result = await service.UpdateTargetDemand("project", "wf", new List<(string, double)>());

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateTargetDemand_CalculationThrowsInvalidOperation_ReturnsBadRequest()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);
        var project = CreateProject(id: 7, puid: "project");
        var workflow = CreateWorkflow(id: 3, projectId: 7, puid: "wf");

        A.CallTo(() => projectRepo.GetProjectByPuid("project")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("wf")).Returns(workflow);
        A.CallTo(() => workflowNodeService.UpsertRootDemands(A<Workflow>._, A<List<(string productPuid, double rate)>>._))
            .ThrowsAsync(new InvalidOperationException("nope"));

        var result = await service.UpdateTargetDemand("project", "wf", new List<(string, double)> { ("p1", 1.0) });

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task UpdateTargetDemand_ValidRequest_ReturnsChartResponse()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var workflowNodeService = A.Fake<IWorkflowNodeService>();
        var service = CreateService(repo, projectRepo, workflowNodeService);
        var project = CreateProject(id: 7, puid: "project");
        var workflow = CreateWorkflow(id: 3, projectId: 7, puid: "wf");
        var chart = new WorkflowChartResponse
        {
            Nodes = new List<WorkflowNodeResponse>(),
            Edges = new List<WorkflowEdgeResponse>(),
            Targets = new List<WorkflowTargetExchange>(),
            ProductNodes = new List<WorkflowProductNodeResponse>()
        };

        A.CallTo(() => projectRepo.GetProjectByPuid("project")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("wf")).Returns(workflow);
        A.CallTo(() => workflowNodeService.UpsertRootDemands(A<Workflow>._, A<List<(string productPuid, double rate)>>._))
            .Returns(chart);

        var result = await service.UpdateTargetDemand("project", "wf", new List<(string, double)> { ("p1", 1.0) });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }
}
