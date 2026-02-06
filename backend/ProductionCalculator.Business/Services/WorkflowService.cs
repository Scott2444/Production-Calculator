
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Helpers;

/**
 * Project last modified date should be updated when products are written to
 * !
 * !
 * ! 
 * !
*/

namespace ProductionCalculator.Business.Services
{
	public class WorkflowService : IWorkflowService
	{
		private readonly ICurrentUserService _currentUser;
		private readonly IWorkflowRepository _repo;
		private readonly IProjectRepository _projectRepo;
		private readonly IWorkflowNodeService _workflowNodeService;

		public WorkflowService(ICurrentUserService currentUser, IWorkflowRepository repo, IProjectRepository projectRepo, IWorkflowNodeService workflowNodeService)
		{
			_currentUser = currentUser;
			_repo = repo;
			_projectRepo = projectRepo;
			_workflowNodeService = workflowNodeService;
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
			return ServiceResult<Workflow>.SuccessResult(workflow, ServiceStatus.Created201);
		}
        public async Task<ServiceResult<Workflow>> UpdateWorkflow(string projectPuid, string puid, string? name, string? description)
		{
			if (string.IsNullOrWhiteSpace(name)) return ServiceResult<Workflow>.Fail(ServiceStatus.BadRequest400, "Workflow name is required.");

            // Get projectId from projectPuid
			var project = await _projectRepo.GetProjectByPuid(projectPuid);
			if (project == null) return ServiceResult<Workflow>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Check if product exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
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

			return ServiceResult.SuccessResult(ServiceStatus.NoContent204);
		}

		public async Task<ServiceResult<WorkflowChartResponse>> UpdateTargetDemand(string projectPuid, string workflowPuid, List<(string productPuid, double rate)> rootDemands)
		{
			var project = await _projectRepo.GetProjectByPuid(projectPuid);
			if (project == null) return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.NotFound404, "Project not found.");

			var workflow = await _repo.GetWorkflowByPuid(workflowPuid);
			if (workflow == null || workflow.Project_Id != project.Project_Id) return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.NotFound404, "Workflow not found.");

			try
			{
				var chart = await _workflowNodeService.UpsertRootDemands(workflow, rootDemands);
				return ServiceResult<WorkflowChartResponse>.SuccessResult(chart);
			}
			catch (InvalidOperationException ex)
			{
				Console.WriteLine(ex);
				return ServiceResult<WorkflowChartResponse>.Fail(ServiceStatus.BadRequest400, $"No possible workflow configuration for the given target demands. {ex.Message}");
			}
		}
	}
}
