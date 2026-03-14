using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.APIModels;
using Microsoft.AspNetCore.Authorization;

namespace ProductionCalculator.API.Controllers
{
    [Route("projects/{projectPuid}/[controller]")]
    public class MachinesController : ApiControllerBase
    {
        private readonly IMachineService _service;

        public MachinesController(IMachineService service)
        {
            _service = service;
        }
        [Authorize(Policy = "IsPublic")]
        [HttpGet("{machinePuid}")]
        public async Task<IActionResult> GetMachineByPuid(string projectPuid, string machinePuid)
        {
            var result = await _service.GetMachineByPuid(projectPuid, machinePuid);
            return FromServiceResult(result, r => r);
        }
        [Authorize(Policy = "IsPublic")]
        [HttpGet]
        public async Task<IActionResult> GetMachinesByProjectPuid(string projectPuid)
        {
            var result = await _service.GetMachinesByProjectPuid(projectPuid);
            return FromServiceResult(result, r => r);
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpPost]
        public async Task<IActionResult> AddMachine(string projectPuid, [FromBody] MachineRequest req)
        {
            var result = await _service.AddMachine(projectPuid, req.Name, req.Description, req.BaseSpeed, req.RecipePuids, req.Attributes);
            return FromServiceResult(result, (r) => r);
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpPut("{machinePuid}")]
        public async Task<IActionResult> UpdateMachine(string projectPuid, string machinePuid, [FromBody] MachineRequest req)
        {
            var result = await _service.UpdateMachine(projectPuid, machinePuid, req.Name, req.Description, req.BaseSpeed, req.RecipePuids, req.Attributes);
            return FromServiceResult(result, r => r);
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpDelete("{machinePuid}")]
        public async Task<IActionResult> DeleteMachine(string projectPuid, string machinePuid)
        {
            var result = await _service.DeleteMachine(projectPuid, machinePuid);
            return FromServiceResult(result);
        }
    }
}
