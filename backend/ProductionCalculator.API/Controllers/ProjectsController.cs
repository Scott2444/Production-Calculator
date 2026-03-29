using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.APIModels;
using Microsoft.AspNetCore.Authorization;

namespace ProductionCalculator.API.Controllers
{
    [Route("[controller]")]
    public class ProjectsController : ApiControllerBase
    {
        private readonly IProjectService _service;

        public ProjectsController(IProjectService service)
        {
            _service = service;
        }

        [Authorize(Policy = "IsUser")]
        [HttpPost]
        public async Task<IActionResult> AddProject([FromBody] ProjectRequest req)
        {
            var result = await _service.AddProject(req.Name, req.Description, req.IsPublic, req.AliasProjectPuid);

            return FromServiceResult(result);
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpPut("{projectPuid}")]
        public async Task<IActionResult> UpdateProject(string projectPuid, [FromBody] ProjectRequest req)
        {
            var result = await _service.UpdateProject(projectPuid, req.Name, req.Description, req.IsPublic, req.AliasProjectPuid);

            return FromServiceResult(result);
        }

        [Authorize(Policy = "IsPublic")]
        [HttpGet("{projectPuid}")]
        public async Task<IActionResult> GetProjectByPuid(string projectPuid)
        {
            var result = await _service.GetProjectByPuid(projectPuid);
            return FromServiceResult(result);
        }

        [Authorize(Policy = "IsUser")]
        [HttpGet("search/public")]
        public async Task<IActionResult> SearchPublicProjects([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _service.SearchPublicProjects(query, page, pageSize);
            return FromServiceResult(result);
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpDelete("{projectPuid}")]
        public async Task<IActionResult> DeleteProject(string projectPuid)
        {
            var result = await _service.DeleteProject(projectPuid);
            return FromServiceResult(result);
        }
    }
}
