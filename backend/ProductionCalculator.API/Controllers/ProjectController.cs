using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.APIModels;
using Microsoft.AspNetCore.Authorization;

namespace ProductionCalculator.API.Controllers
{
    [Route("projects")]
    public class ProjectController : ApiControllerBase
    {
        private readonly IProjectService _service;

        public ProjectController(IProjectService service)
        {
            _service = service;
        }

        [Authorize(Policy = "IsUser")]
        [HttpPost]
        public async Task<IActionResult> AddProject([FromBody] AddProjectRequest req)
        {
            var result = await _service.AddProject(req.Name, req.Description);

            return FromServiceResult(result, (p) => new ProjectResponse { Puid = p.Puid, Name = p.Name, Description = p.Description, CreatedAt = p.Created_At, UpdatedAt = p.Last_Updated});
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpGet("{puid}")]
        public async Task<IActionResult> GetProjectByPuid(string puid)
        {
            var result = await _service.GetProjectByPuid(puid);
            return FromServiceResult(result, p => new ProjectResponse { Puid = p.Puid, Name = p.Name, Description = p.Description, CreatedAt = p.Created_At, UpdatedAt = p.Last_Updated});
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpGet("users/{puid}")]
        public async Task<IActionResult> GetProjectsbyUserPuid(string puid)
        {
            var result = await _service.GetProjectsByUserPuid(puid);
            return FromServiceResult(result, projects => projects.Select(p => new ProjectResponse { Puid = p.Puid, Name = p.Name, Description = p.Description, CreatedAt = p.Created_At, UpdatedAt = p.Last_Updated }).ToList());
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpDelete("{puid}")]
        public async Task<IActionResult> DeleteProject(string puid)
        {
            var result = await _service.DeleteProject(puid);
            return FromServiceResult(result);
        }
    }
}
