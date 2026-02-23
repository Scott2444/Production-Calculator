using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.APIModels;
using Microsoft.AspNetCore.Authorization;

namespace ProductionCalculator.API.Controllers
{
    [Route("api/projects/{projectPuid}/[controller]")]
    public class RecipesController : ApiControllerBase
    {
        private readonly IRecipeService _service;

        public RecipesController(IRecipeService service)
        {
            _service = service;
        }
        [Authorize(Policy = "IsPublic")]
        [HttpGet("{recipePuid}")]
        public async Task<IActionResult> GetRecipeByPuid(string projectPuid, string recipePuid)
        {
            var result = await _service.GetRecipeByPuid(projectPuid, recipePuid);
            return FromServiceResult(result, r => r);
        }
        [Authorize(Policy = "IsPublic")]
        [HttpGet]
        public async Task<IActionResult> GetRecipesByProjectPuid(string projectPuid)
        {
            var result = await _service.GetRecipesByProjectPuid(projectPuid);
            return FromServiceResult(result, r => r);
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpPost]
        public async Task<IActionResult> AddRecipe(string projectPuid, [FromBody] RecipeRequest req)
        {
            var result = await _service.AddRecipe(projectPuid, req.Name, req.Description, req.BaseCraftingTime, req.Inputs, req.Outputs, req.Attributes);
            return FromServiceResult(result, (r) => r);
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpPut("{recipePuid}")]
        public async Task<IActionResult> UpdateRecipe(string projectPuid, string recipePuid, [FromBody] RecipeRequest req)
        {
            var result = await _service.UpdateRecipe(projectPuid, recipePuid, req.Name, req.Description, req.BaseCraftingTime, req.Inputs, req.Outputs, req.Attributes);
            return FromServiceResult(result, r => r);
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpDelete("{recipePuid}")]
        public async Task<IActionResult> DeleteRecipe(string projectPuid, string recipePuid)
        {
            var result = await _service.DeleteRecipe(projectPuid, recipePuid);
            return FromServiceResult(result);
        }
    }
}
