using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.API.Controllers
{
    [Route("api/projects/{projectPuid}/[controller]")]
    public class AttributesController : ApiControllerBase
    {
        private readonly IAttributeService _service;

        public AttributesController(IAttributeService service)
        {
            _service = service;
        }

        [Authorize(Policy = "IsPublic")]
        [HttpGet("{attributePuid}")]
        public async Task<IActionResult> GetAttributeByPuid(string projectPuid, string attributePuid)
        {
            var result = await _service.GetAttributeByPuid(projectPuid, attributePuid);
            return FromServiceResult(result, a => new AttributeResponse
            {
                Puid = a.Puid,
                Name = a.Name,
                Description = a.Description,
                Unit = a.Unit,
                CreatedAt = a.Created_At,
                UpdatedAt = a.Last_Updated
            });
        }

        [Authorize(Policy = "IsPublic")]
        [HttpGet]
        public async Task<IActionResult> GetAttributesByProjectPuid(string projectPuid)
        {
            var result = await _service.GetAttributesByProjectPuid(projectPuid);
            return FromServiceResult(result, attributes => attributes.Select(a => new AttributeResponse
            {
                Puid = a.Puid,
                Name = a.Name,
                Description = a.Description,
                Unit = a.Unit,
                CreatedAt = a.Created_At,
                UpdatedAt = a.Last_Updated
            }).ToList());
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpPost]
        public async Task<IActionResult> AddAttribute(string projectPuid, [FromBody] AttributeRequest req)
        {
            var result = await _service.AddAttribute(projectPuid, req.Name, req.Description, req.Unit);
            return FromServiceResult(result, a => new AttributeResponse
            {
                Puid = a.Puid,
                Name = a.Name,
                Description = a.Description,
                Unit = a.Unit,
                CreatedAt = a.Created_At,
                UpdatedAt = a.Last_Updated
            });
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpPut("{attributePuid}")]
        public async Task<IActionResult> UpdateAttribute(string projectPuid, string attributePuid, [FromBody] AttributeRequest req)
        {
            var result = await _service.UpdateAttribute(projectPuid, attributePuid, req.Name, req.Description, req.Unit);
            return FromServiceResult(result, a => new AttributeResponse
            {
                Puid = a.Puid,
                Name = a.Name,
                Description = a.Description,
                Unit = a.Unit,
                CreatedAt = a.Created_At,
                UpdatedAt = a.Last_Updated
            });
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpDelete("{attributePuid}")]
        public async Task<IActionResult> DeleteAttribute(string projectPuid, string attributePuid)
        {
            var result = await _service.DeleteAttribute(projectPuid, attributePuid);
            return FromServiceResult(result);
        }
    }
}
