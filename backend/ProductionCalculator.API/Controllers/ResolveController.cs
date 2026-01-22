using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.APIModels;
using Microsoft.AspNetCore.Authorization;

namespace ProductionCalculator.API.Controllers
{
    [Route("api/[controller]")]
    public class ResolveController : ApiControllerBase
    {
        private readonly IProjectService _service;

        public ResolveController(IProjectService service)
        {
            _service = service;
        }

        [Authorize(Policy = "None")]  // Requies custom auth logic in service
        [HttpGet("projects")]
        public async Task<IActionResult> ResolveProject([FromQuery] string username, [FromQuery] string project)
        {
            var result = await _service.ResolveProject(username, project);
            return FromServiceResult(result, p => new ProjectResolveResponse { ProjectPuid = p.Puid });
        }
    }
}
