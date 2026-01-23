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
                FlatSpeedBonus = r.Flat_Speed_Bonus,
                AdditivePercentBonus = r.Additive_Percent_Bonus,
                MultiplicativeModifier = r.Multiplicative_Modifiers,
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
                FlatSpeedBonus = m.Flat_Speed_Bonus,
                AdditivePercentBonus = m.Additive_Percent_Bonus,
                MultiplicativeModifier = m.Multiplicative_Modifiers,
                CreatedAt = m.Created_At,
                UpdatedAt = m.Last_Updated
            }).ToList());
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpPost]
        public async Task<IActionResult> AddModifier(string projectPuid, [FromBody] ModifierRequest req)
        {
            var result = await _service.AddModifier(projectPuid, req.Name, req.Description, req.FlatSpeedBonus, req.AdditivePercentBonus, req.MultiplicativeModifier);
            return FromServiceResult(result, (r) => new ModifierResponse
            {
                Puid = r.Puid,
                Name = r.Name,
                Description = r.Description,
                FlatSpeedBonus = r.Flat_Speed_Bonus,
                AdditivePercentBonus = r.Additive_Percent_Bonus,
                MultiplicativeModifier = r.Multiplicative_Modifiers,
                CreatedAt = r.Created_At,
                UpdatedAt = r.Last_Updated
            });
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpPut("{modifierPuid}")]
        public async Task<IActionResult> UpdateModifier(string projectPuid, string modifierPuid, [FromBody] ModifierRequest req)
        {
            var result = await _service.UpdateModifier(projectPuid, modifierPuid, req.Name, req.Description, req.FlatSpeedBonus, req.AdditivePercentBonus, req.MultiplicativeModifier);
            return FromServiceResult(result, r => new ModifierResponse
            {
                Puid = r.Puid,
                Name = r.Name,
                Description = r.Description,
                FlatSpeedBonus = r.Flat_Speed_Bonus,
                AdditivePercentBonus = r.Additive_Percent_Bonus,
                MultiplicativeModifier = r.Multiplicative_Modifiers,
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
