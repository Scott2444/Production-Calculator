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
            var result = await _service.AddProject(req.Name, req.Description, req.IsPublic, req.AliasProjectPuid);

            return FromServiceResult(result, (p) => new ProjectResponse { Puid = p.Puid, Name = p.Name, Description = p.Description, IsPublic = p.Is_Public, AliasProjectPuid = p.Alias_Project_Puid, CreatedAt = p.Created_At, UpdatedAt = p.Last_Updated });
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpPut("{projectPuid}")]
        public async Task<IActionResult> UpdateProject(string projectPuid, [FromBody] AddProjectRequest req)
        {
            var result = await _service.UpdateProject(projectPuid, req.Name, req.Description, req.IsPublic, req.AliasProjectPuid);

            return FromServiceResult(result, (p) => new ProjectResponse { Puid = p.Puid, Name = p.Name, Description = p.Description, IsPublic = p.Is_Public, AliasProjectPuid = p.Alias_Project_Puid, CreatedAt = p.Created_At, UpdatedAt = p.Last_Updated });
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpGet("{projectPuid}")]
        public async Task<IActionResult> GetProjectByPuid(string projectPuid)
        {
            var result = await _service.GetProjectByPuid(projectPuid);
            return FromServiceResult(result, p => new ProjectResponse { Puid = p.Puid, Name = p.Name, Description = p.Description, IsPublic = p.Is_Public, AliasProjectPuid = p.Alias_Project_Puid, CreatedAt = p.Created_At, UpdatedAt = p.Last_Updated });
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
