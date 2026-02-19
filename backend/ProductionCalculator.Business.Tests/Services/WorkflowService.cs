using FakeItEasy;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Services;
using ProductionCalculator.Business.APIModels;

namespace ProductionCalculator.Business.Tests;

public class WorkflowServiceTests
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

    private static Workflow CreateWorkflow(int id = 1, int projectId = 1, string puid = "wf123", string name = "Workflow")
    {
        return new Workflow
        {
            Workflow_Id = id,
            Project_Id = projectId,
            Puid = puid,
            Name = name,
            Description = "Description",
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    private static WorkflowService CreateService(IWorkflowRepository repo, IProjectRepository projectRepo, IWorkflowChartService nodeService)
    {
        var currentUser = A.Fake<ICurrentUserService>();
        return new WorkflowService(currentUser, repo, projectRepo, nodeService);
    }

    [Fact]
    public async Task AddWorkflow_EmptyName_ReturnsBadRequest()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);

        var result = await service.AddWorkflow("projectPuid", "", "desc");

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task AddWorkflow_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.AddWorkflow("missing", "WF", "desc");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task AddWorkflow_DuplicateNameInProject_ReturnsConflict()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetWorkflowsByProjectId(10)).Returns(new List<Workflow> { CreateWorkflow(name: "Existing") });

        var result = await service.AddWorkflow("projPuid", "Existing", "desc");

        Assert.Equal(ServiceStatus.Conflict409, result.Status);
    }

    [Fact]
    public async Task AddWorkflow_ValidRequest_ReturnsCreatedAndSavesToRepo()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);
        var project = CreateProject(id: 10, puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetWorkflowsByProjectId(10)).Returns(new List<Workflow>());
        A.CallTo(() => repo.PuidExists(A<string>._)).Returns(false);

        var result = await service.AddWorkflow("projPuid", "NewWF", "desc");

        Assert.True(result.Success);
        Assert.Equal(ServiceStatus.Created201, result.Status);
        A.CallTo(() => repo.AddWorkflow(A<Workflow>.That.Matches(w => w.Name == "NewWF" && w.Project_Id == 10))).MustHaveHappenedOnceExactly();
        A.CallTo(() => projectRepo.UpdateProject(A<Project>.That.Matches(p => p.Project_Id == 10))).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UpdateWorkflow_EmptyName_ReturnsBadRequest()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);

        var result = await service.UpdateWorkflow("projPuid", "wfPuid", "", "desc");

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
    }

    [Fact]
    public async Task UpdateWorkflow_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.UpdateWorkflow("missing", "wfPuid", "New Name", "desc");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateWorkflow_WorkflowNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);
        var project = CreateProject(puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("missing")).Returns(Task.FromResult<Workflow?>(null));

        var result = await service.UpdateWorkflow("projPuid", "missing", "New Name", "desc");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateWorkflow_WorkflowBelongsToDifferentProject_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);
        var project = CreateProject(id: 10, puid: "projPuid");
        var workflow = CreateWorkflow(projectId: 20, puid: "wfPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("wfPuid")).Returns(workflow);

        var result = await service.UpdateWorkflow("projPuid", "wfPuid", "New Name", "desc");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateWorkflow_DuplicateNameOtherThanSelf_ReturnsConflict()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);
        var project = CreateProject(id: 10, puid: "projPuid");
        var workflow = CreateWorkflow(id: 1, projectId: 10, puid: "wfPuid", name: "MyWF");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("wfPuid")).Returns(workflow);
        A.CallTo(() => repo.GetWorkflowsByProjectId(10)).Returns(new List<Workflow> { CreateWorkflow(id: 2, name: "OtherWF") });

        var result = await service.UpdateWorkflow("projPuid", "wfPuid", "OtherWF", "desc");

        Assert.Equal(ServiceStatus.Conflict409, result.Status);
    }

    [Fact]
    public async Task UpdateWorkflow_ValidRequest_ReturnsSuccessAndUpdatesRepo()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);
        var project = CreateProject(id: 10, puid: "projPuid");
        var workflow = CreateWorkflow(id: 1, projectId: 10, puid: "wfPuid", name: "OldName");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("wfPuid")).Returns(workflow);
        A.CallTo(() => repo.GetWorkflowsByProjectId(10)).Returns(new List<Workflow> { workflow });

        var result = await service.UpdateWorkflow("projPuid", "wfPuid", "NewName", "NewDesc");

        Assert.True(result.Success);
        Assert.Equal("NewName", workflow.Name);
        A.CallTo(() => repo.UpdateWorkflow(workflow)).MustHaveHappenedOnceExactly();
        A.CallTo(() => projectRepo.UpdateProject(project)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetWorkflowByPuid_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.GetWorkflowByPuid("missing", "wfPuid");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetWorkflowByPuid_WorkflowNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);
        var project = CreateProject(puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("missing")).Returns(Task.FromResult<Workflow?>(null));

        var result = await service.GetWorkflowByPuid("projPuid", "missing");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetWorkflowByPuid_WorkflowBelongsToDifferentProject_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);
        var project = CreateProject(id: 10, puid: "projPuid");
        var workflow = CreateWorkflow(projectId: 20, puid: "wfPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("wfPuid")).Returns(workflow);

        var result = await service.GetWorkflowByPuid("projPuid", "wfPuid");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetWorkflowByPuid_ValidInputs_ReturnsWorkflow()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);
        var project = CreateProject(id: 10, puid: "projPuid");
        var workflow = CreateWorkflow(projectId: 10, puid: "wfPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("wfPuid")).Returns(workflow);

        var result = await service.GetWorkflowByPuid("projPuid", "wfPuid");

        Assert.True(result.Success);
        Assert.Equal(workflow, result.Data);
    }

    [Fact]
    public async Task GetWorkflowsByProjectPuid_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.GetWorkflowsByProjectPuid("missing");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task GetWorkflowsByProjectPuid_ProjectExists_ReturnsWorkflowList()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);
        var project = CreateProject(id: 10, puid: "projPuid");
        var workflows = new List<Workflow> { CreateWorkflow(projectId: 10) };
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetWorkflowsByProjectId(10)).Returns(workflows);

        var result = await service.GetWorkflowsByProjectPuid("projPuid");

        Assert.True(result.Success);
        Assert.Equal(workflows, result.Data);
    }

    [Fact]
    public async Task DeleteWorkflow_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.DeleteWorkflow("missing", "wfPuid");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task DeleteWorkflow_WorkflowNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);
        var project = CreateProject(puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("missing")).Returns(Task.FromResult<Workflow?>(null));

        var result = await service.DeleteWorkflow("projPuid", "missing");

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task DeleteWorkflow_RepoReturnsFalse_ReturnsInternalServerError()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);
        var project = CreateProject(id: 10, puid: "projPuid");
        var workflow = CreateWorkflow(id: 1, projectId: 10, puid: "wfPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("wfPuid")).Returns(workflow);
        A.CallTo(() => repo.DeleteWorkflow(1)).Returns(false);

        var result = await service.DeleteWorkflow("projPuid", "wfPuid");

        Assert.Equal(ServiceStatus.InternalServerError500, result.Status);
    }

    [Fact]
    public async Task DeleteWorkflow_ValidRequest_ReturnsNoContent()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);
        var project = CreateProject(id: 10, puid: "projPuid");
        var workflow = CreateWorkflow(id: 1, projectId: 10, puid: "wfPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("wfPuid")).Returns(workflow);
        A.CallTo(() => repo.DeleteWorkflow(1)).Returns(true);

        var result = await service.DeleteWorkflow("projPuid", "wfPuid");

        Assert.Equal(ServiceStatus.NoContent204, result.Status);
        A.CallTo(() => projectRepo.UpdateProject(project)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UpdateTargetDemand_ProjectNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);
        A.CallTo(() => projectRepo.GetProjectByPuid("missing")).Returns(Task.FromResult<Project?>(null));

        var result = await service.UpdateTargetDemand("missing", "wfPuid", new List<(string, double)>());

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateTargetDemand_WorkflowNotFound_ReturnsNotFound()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);
        var project = CreateProject(puid: "projPuid");
        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("missing")).Returns(Task.FromResult<Workflow?>(null));

        var result = await service.UpdateTargetDemand("projPuid", "missing", new List<(string, double)>());

        Assert.Equal(ServiceStatus.NotFound404, result.Status);
    }

    [Fact]
    public async Task UpdateTargetDemand_ValidRequest_ReturnsSuccess()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);
        var project = CreateProject(id: 10, puid: "projPuid");
        var workflow = CreateWorkflow(projectId: 10, puid: "wfPuid");
        var demands = new List<(string, double)> { ("prod1", 10.0) };
        var chart = new WorkflowChartResponse { Nodes = [], Edges = [], Targets = [], ProductNodes = [], PreferredRecipes = [] };

        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("wfPuid")).Returns(workflow);
        A.CallTo(() => nodeService.UpsertRootDemands(workflow, demands)).Returns(ServiceResult<WorkflowChartResponse>.SuccessResult(chart));

        var result = await service.UpdateTargetDemand("projPuid", "wfPuid", demands);

        Assert.True(result.Success);
        Assert.Equal(chart, result.Data);
    }

    [Fact]
    public async Task UpdateTargetDemand_InvalidOperationException_ReturnsBadRequest()
    {
        var repo = A.Fake<IWorkflowRepository>();
        var projectRepo = A.Fake<IProjectRepository>();
        var nodeService = A.Fake<IWorkflowChartService>();
        var service = CreateService(repo, projectRepo, nodeService);
        var project = CreateProject(id: 10, puid: "projPuid");
        var workflow = CreateWorkflow(projectId: 10, puid: "wfPuid");
        var demands = new List<(string, double)> { ("prod1", 10.0) };

        A.CallTo(() => projectRepo.GetProjectByPuid("projPuid")).Returns(project);
        A.CallTo(() => repo.GetWorkflowByPuid("wfPuid")).Returns(workflow);
        A.CallTo(() => nodeService.UpsertRootDemands(workflow, demands)).Returns(ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.BadRequest400, "Infeasible"));

        var result = await service.UpdateTargetDemand("projPuid", "wfPuid", demands);

        Assert.Equal(ServiceStatus.BadRequest400, result.Status);
        Assert.Contains("Infeasible", result.ErrorMessage!);
    }
}
