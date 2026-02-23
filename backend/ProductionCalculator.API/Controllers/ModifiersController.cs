using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.APIModels;
using Microsoft.AspNetCore.Authorization;

namespace ProductionCalculator.API.Controllers
{
    [Route("api/projects/{projectPuid}/[controller]")]
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
            return FromServiceResult(result, r => new ModifierResponse
            {
                Puid = r.Puid,
                Name = r.Name,
                Description = r.Description,
                FlatBonus = r.Flat_Bonus,
                PercentBonus = r.Percent_Bonus,
                MultiplicativeBonus = r.Multiplicative_Bonus,
                InputPercent = r.Input_Percent,
                OutputPercent = r.Output_Percent,
                Attributes = [],
                CreatedAt = r.Created_At,
                UpdatedAt = r.Last_Updated
            });
        }
        [Authorize(Policy = "IsPublic")]
        [HttpGet]
        public async Task<IActionResult> GetModifiersByProjectPuid(string projectPuid)
        {
            var result = await _service.GetModifiersByProjectPuid(projectPuid);
            return FromServiceResult(result, r => r.Select(m => new ModifierResponse
            {
                Puid = m.Puid,
                Name = m.Name,
                Description = m.Description,
                FlatBonus = m.Flat_Bonus,
                PercentBonus = m.Percent_Bonus,
                MultiplicativeBonus = m.Multiplicative_Bonus,
                InputPercent = m.Input_Percent,
                OutputPercent = m.Output_Percent,
                Attributes = [],
                CreatedAt = m.Created_At,
                UpdatedAt = m.Last_Updated
            }).ToList());
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpPost]
        public async Task<IActionResult> AddModifier(string projectPuid, [FromBody] ModifierRequest req)
        {
            var result = await _service.AddModifier(projectPuid, req.Name, req.Description, req.FlatBonus, req.PercentBonus, req.MultiplicativeBonus, req.InputPercent, req.OutputPercent, req.Attributes);
            return FromServiceResult(result, (r) => new ModifierResponse
            {
                Puid = r.Puid,
                Name = r.Name,
                Description = r.Description,
                FlatBonus = r.Flat_Bonus,
                PercentBonus = r.Percent_Bonus,
                MultiplicativeBonus = r.Multiplicative_Bonus,
                InputPercent = r.Input_Percent,
                OutputPercent = r.Output_Percent,
                Attributes = req.Attributes,
                CreatedAt = r.Created_At,
                UpdatedAt = r.Last_Updated
            });
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpPut("{modifierPuid}")]
        public async Task<IActionResult> UpdateModifier(string projectPuid, string modifierPuid, [FromBody] ModifierRequest req)
        {
            var result = await _service.UpdateModifier(projectPuid, modifierPuid, req.Name, req.Description, req.FlatBonus, req.PercentBonus, req.MultiplicativeBonus, req.InputPercent, req.OutputPercent, req.Attributes);
            return FromServiceResult(result, r => new ModifierResponse
            {
                Puid = r.Puid,
                Name = r.Name,
                Description = r.Description,
                FlatBonus = r.Flat_Bonus,
                PercentBonus = r.Percent_Bonus,
                MultiplicativeBonus = r.Multiplicative_Bonus,
                InputPercent = r.Input_Percent,
                OutputPercent = r.Output_Percent,
                Attributes = req.Attributes,
                CreatedAt = r.Created_At,
                UpdatedAt = r.Last_Updated
            });
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
