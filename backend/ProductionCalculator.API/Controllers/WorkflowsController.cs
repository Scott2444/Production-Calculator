using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.APIModels;
using Microsoft.AspNetCore.Authorization;

namespace ProductionCalculator.API.Controllers
{
    [Route("api/projects/{projectPuid}/[controller]")]
    public class WorkflowsController : ApiControllerBase
    {
        private readonly IWorkflowService _service;

        public WorkflowsController(IWorkflowService service)
        {
            _service = service;
        }
        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpGet("{workflowPuid}")]
        public async Task<IActionResult> GetWorkflowByPuid(string projectPuid, string workflowPuid)
        {
            var result = await _service.GetWorkflowByPuid(projectPuid, workflowPuid);
            return FromServiceResult(result, p => new WorkflowResponse { Puid = p.Puid, Name = p.Name, Description = p.Description, CreatedAt = p.Created_At, UpdatedAt = p.Last_Updated});
        }
        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpGet]
        public async Task<IActionResult> GetWorkflowsByProjectPuid(string projectPuid)
        {
            var result = await _service.GetWorkflowsByProjectPuid(projectPuid);
            return FromServiceResult(result, workflows => workflows.Select(w => new WorkflowResponse { Puid = w.Puid, Name = w.Name, Description = w.Description, CreatedAt = w.Created_At, UpdatedAt = w.Last_Updated}).ToList());
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpPost]
        public async Task<IActionResult> AddWorkflow(string projectPuid, [FromBody] WorkflowRequest req)
        {
            var result = await _service.AddWorkflow(projectPuid, req.Name, req.Description);
            return FromServiceResult(result, (p) => new WorkflowResponse { Puid = p.Puid, Name = p.Name, Description = p.Description, CreatedAt = p.Created_At, UpdatedAt = p.Last_Updated});
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpPut("{workflowPuid}")]
        public async Task<IActionResult> UpdateWorkflow(string projectPuid, string workflowPuid, [FromBody] WorkflowRequest req)
        {
            var result = await _service.UpdateWorkflow(projectPuid, workflowPuid, req.Name, req.Description);
            return FromServiceResult(result, p => new WorkflowResponse { Puid = p.Puid, Name = p.Name, Description = p.Description, CreatedAt = p.Created_At, UpdatedAt = p.Last_Updated});
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpDelete("{workflowPuid}")]
        public async Task<IActionResult> DeleteWorkflow(string projectPuid, string workflowPuid)
        {
            var result = await _service.DeleteWorkflow(projectPuid, workflowPuid);
            return FromServiceResult(result);
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpPut("{workflowPuid}/target-demand")]
        public async Task<IActionResult> UpdateTargetDemand(string projectPuid, string workflowPuid, [FromBody] WorkflowTargetRequest request)
        {
            var result = await _service.UpdateTargetDemand(projectPuid, workflowPuid, request.Targets.Select(t => (t.ProductPuid, t.TargetRate)).ToList());
            return FromServiceResult(result, r => r);
        }
    }
}
