using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.APIModels;
using Microsoft.AspNetCore.Authorization;

namespace ProductionCalculator.API.Controllers
{
    [Route("projects/{projectPuid}/[controller]")]
    public class ModifiersController : ApiControllerBase
    {
        private readonly IModifierService _service;

        public ModifiersController(IModifierService service)
        {
            _service = service;
        }
        [Authorize(Policy = "IsPublic")]
        [HttpGet("{modifierPuid}")]
        public async Task<IActionResult> GetModifierByPuid(string projectPuid, string modifierPuid)
        {
            var result = await _service.GetModifierByPuid(projectPuid, modifierPuid);
            return FromServiceResult(result, r => r);
        }
        [Authorize(Policy = "IsPublic")]
        [HttpGet]
        public async Task<IActionResult> GetModifiersByProjectPuid(string projectPuid)
        {
            var result = await _service.GetModifiersByProjectPuid(projectPuid);
            return FromServiceResult(result, r => r);
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpPost]
        public async Task<IActionResult> AddModifier(string projectPuid, [FromBody] ModifierRequest req)
        {
            var result = await _service.AddModifier(projectPuid, req.Name, req.Description, req.FlatBonus, req.PercentBonus, req.MultiplicativeBonus, req.InputPercent, req.OutputPercent, req.Attributes);
            return FromServiceResult(result, r => r);
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpPut("{modifierPuid}")]
        public async Task<IActionResult> UpdateModifier(string projectPuid, string modifierPuid, [FromBody] ModifierRequest req)
        {
            var result = await _service.UpdateModifier(projectPuid, modifierPuid, req.Name, req.Description, req.FlatBonus, req.PercentBonus, req.MultiplicativeBonus, req.InputPercent, req.OutputPercent, req.Attributes);
            return FromServiceResult(result, r => r);
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpDelete("{modifierPuid}")]
        public async Task<IActionResult> DeleteModifier(string projectPuid, string modifierPuid)
        {
            var result = await _service.DeleteModifier(projectPuid, modifierPuid);
            return FromServiceResult(result);
        }
    }
}
