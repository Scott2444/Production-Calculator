using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Helpers;

namespace ProductionCalculator.Business.Services
{
	public class WorkflowService : IWorkflowService
	{
		private readonly ICurrentUserService _currentUser;
		private readonly IWorkflowRepository _repo;
		private readonly IProjectRepository _projectRepo;
		private readonly IWorkflowChartService _workflowChartService;

		public WorkflowService(ICurrentUserService currentUser, IWorkflowRepository repo, IProjectRepository projectRepo, IWorkflowChartService workflowChartService)
		{
			_currentUser = currentUser;
			_repo = repo;
			_projectRepo = projectRepo;
			_workflowChartService = workflowChartService;
		}

		public async Task<ServiceResult<Workflow>> AddWorkflow(string projectPuid, string name, string? description)
		{
			if (string.IsNullOrWhiteSpace(name)) return ServiceResult<Workflow>.Fail(ServiceStatus.BadRequest400, "Workflow name is required.");

            // Get projectId from projectPuid
			var project = await _projectRepo.GetProjectByPuid(projectPuid);
			if (project == null) return ServiceResult<Workflow>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Check if name already exists for this project
			var existingWorkflows = await _repo.GetWorkflowsByProjectId(project.Project_Id);
			if (existingWorkflows.Any(w => w.Name == name)) return ServiceResult<Workflow>.Fail(ServiceStatus.Conflict409, "Workflow name already exists for this project.");

            // Limit string lengths
			name = TruncateHelper.TruncateString(name, 255);
			description = TruncateHelper.TruncateStringNullable(description, 1000);

			var puid = await PuidHelper.GenerateUniquePuidAsync(_repo.PuidExists);

			var workflow = new Workflow
			{
				Workflow_Id = 0,
				Project_Id = project.Project_Id,
				Puid = puid,
				Name = name,
				Description = description ?? string.Empty,
				Created_At = DateTime.UtcNow,
				Last_Updated = DateTime.UtcNow
			};

			await _repo.AddWorkflow(workflow);
			await UpdateProjectLastUpdated(project);
			return ServiceResult<Workflow>.SuccessResult(workflow, ServiceStatus.Created201);
		}
        public async Task<ServiceResult<Workflow>> UpdateWorkflow(string projectPuid, string puid, string? name, string? description)
		{
			if (string.IsNullOrWhiteSpace(name)) return ServiceResult<Workflow>.Fail(ServiceStatus.BadRequest400, "Workflow name is required.");

            // Get projectId from projectPuid
			var project = await _projectRepo.GetProjectByPuid(projectPuid);
			if (project == null) return ServiceResult<Workflow>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Check if workflow exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
			var workflow = await _repo.GetWorkflowByPuid(puid);
			if (workflow == null || workflow.Project_Id != project.Project_Id) return ServiceResult<Workflow>.Fail(ServiceStatus.NotFound404, "Workflow not found.");

            // Check if name already exists for this project
			var existingWorkflows = await _repo.GetWorkflowsByProjectId(project.Project_Id);
			if (existingWorkflows.Any(w => w.Name == name && w.Workflow_Id != workflow.Workflow_Id)) return ServiceResult<Workflow>.Fail(ServiceStatus.Conflict409, "Workflow name already exists for this project.");

            // Limit string lengths
			name = TruncateHelper.TruncateString(name, 255);
			description = TruncateHelper.TruncateStringNullable(description, 1000);

			workflow.Name = name;
			workflow.Description = description;
			workflow.Last_Updated = DateTime.UtcNow;

			await _repo.UpdateWorkflow(workflow);
			await UpdateProjectLastUpdated(project);
			return ServiceResult<Workflow>.SuccessResult(workflow);
		}

		public async Task<ServiceResult<Workflow>> GetWorkflowByPuid(string projectPuid, string puid)
		{
			var project = await _projectRepo.GetProjectByPuid(projectPuid);
			if (project == null) return ServiceResult<Workflow>.Fail(ServiceStatus.NotFound404, "Project not found.");

			var workflow = await _repo.GetWorkflowByPuid(puid);
			if (workflow == null || workflow.Project_Id != project.Project_Id) return ServiceResult<Workflow>.Fail(ServiceStatus.NotFound404, "Workflow not found.");

			return ServiceResult<Workflow>.SuccessResult(workflow);
		}

		public async Task<ServiceResult<List<Workflow>>> GetWorkflowsByProjectPuid(string projectPuid)
		{
			var project = await _projectRepo.GetProjectByPuid(projectPuid);
			if (project == null) return ServiceResult<List<Workflow>>.Fail(ServiceStatus.NotFound404, "Project not found.");

			var workflows = await _repo.GetWorkflowsByProjectId(project.Project_Id);
			return ServiceResult<List<Workflow>>.SuccessResult(workflows);
		}

		public async Task<ServiceResult> DeleteWorkflow(string projectPuid, string puid)
		{
			var project = await _projectRepo.GetProjectByPuid(projectPuid);
			if (project == null) return ServiceResult.Fail(ServiceStatus.NotFound404, "Project not found.");

			var workflow = await _repo.GetWorkflowByPuid(puid);
			if (workflow == null || workflow.Project_Id != project.Project_Id) return ServiceResult.Fail(ServiceStatus.NotFound404, "Workflow not found.");

			var isDeleted = await _repo.DeleteWorkflow(workflow.Workflow_Id);
			if (!isDeleted) return ServiceResult.Fail(ServiceStatus.InternalServerError500, "Failed to delete workflow.");

			await UpdateProjectLastUpdated(project);
			return ServiceResult.SuccessResult(ServiceStatus.NoContent204);
		}

		// --------------------------------------
		// Workflow chart operations
		// --------------------------------------

		public async Task<ServiceResult<WorkflowChartResponse>> GetWorkflowChartById(string projectPuid, string workflowPuid)
		{
			var project = await _projectRepo.GetProjectByPuid(projectPuid);
			if (project == null) return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.NotFound404, "Project not found.");

			// Check if workflow exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
			var workflow = await _repo.GetWorkflowByPuid(workflowPuid);
			if (workflow == null || workflow.Project_Id != project.Project_Id) return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.NotFound404, "Workflow not found.");

			return await _workflowChartService.GetWorkflowChartById(workflow);
		}

        public async Task<ServiceResult<WorkflowChartResponse>> UpdateTargetDemand(string projectPuid, string workflowPuid, List<(string productPuid, double rate)> rootDemands)
		{
			var project = await _projectRepo.GetProjectByPuid(projectPuid);
			if (project == null) return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.NotFound404, "Project not found.");

			// Check if workflow exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
			var workflow = await _repo.GetWorkflowByPuid(workflowPuid);
			if (workflow == null || workflow.Project_Id != project.Project_Id) return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.NotFound404, "Workflow not found.");

			return await _workflowChartService.UpsertRootDemands(workflow, rootDemands);
		}
        public async Task<ServiceResult<WorkflowChartResponse>> UpdateNode(string projectPuid, string workflowPuid, string nodePuid, WorkflowNodeRequest request)
		{
			var project = await _projectRepo.GetProjectByPuid(projectPuid);
			if (project == null) return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.NotFound404, "Project not found.");

			// Check if workflow exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
			var workflow = await _repo.GetWorkflowByPuid(workflowPuid);
			if (workflow == null || workflow.Project_Id != project.Project_Id) return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.NotFound404, "Workflow not found.");

			// WorkflowNodeService will check request fields
			return await _workflowChartService.UpdateNode(workflow, nodePuid, request);
		}
		public async Task<ServiceResult<WorkflowChartResponse>> SetRecipes(string projectPuid, string workflowPuid, List<string> recipePuids)
		{
			var project = await _projectRepo.GetProjectByPuid(projectPuid);
			if (project == null) return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.NotFound404, "Project not found.");

			// Check if workflow exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
			var workflow = await _repo.GetWorkflowByPuid(workflowPuid);
			if (workflow == null || workflow.Project_Id != project.Project_Id) return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.NotFound404, "Workflow not found.");

			return await _workflowChartService.SetRecipes(workflow, recipePuids);
		}
		public async Task<ServiceResult<WorkflowChartResponse>> SetExternal(string projectPuid, string workflowPuid, string productPuid, bool isExternal, double? externalRate)
		{
			var project = await _projectRepo.GetProjectByPuid(projectPuid);
			if (project == null) return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.NotFound404, "Project not found.");

			// Check if workflow exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
			var workflow = await _repo.GetWorkflowByPuid(workflowPuid);
			if (workflow == null || workflow.Project_Id != project.Project_Id) return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.NotFound404, "Workflow not found.");

			return await _workflowChartService.SetExternal(workflow, productPuid, isExternal, externalRate);
		}
        public async Task<ServiceResult<WorkflowChartResponse>> UpgradeWorkflowChart(string projectPuid, string workflowPuid)
		{
			var project = await _projectRepo.GetProjectByPuid(projectPuid);
			if (project == null) return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.NotFound404, "Project not found.");

			// Check if workflow exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
			var workflow = await _repo.GetWorkflowByPuid(workflowPuid);
			if (workflow == null || workflow.Project_Id != project.Project_Id) return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.NotFound404, "Workflow not found.");

			return await _workflowChartService.UpgradeWorkflowChart(workflow);
		}

		private async Task UpdateProjectLastUpdated(Project project)
        {
            project.Last_Updated = DateTime.UtcNow;
            await _projectRepo.UpdateProject(project);
        }
	}
}
