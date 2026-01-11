using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.APIModels;
using Microsoft.AspNetCore.Authorization;

namespace ProductionCalculator.API.Controllers
{
    [Route("api/projects/{projectPuid}/[controller]")]
    public class ProductsController : ApiControllerBase
    {
        private readonly IProductService _service;

        public ProductsController(IProductService service)
        {
            _service = service;
        }
        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpGet("{productPuid}")]
        public async Task<IActionResult> GetProductByPuid(string projectPuid, string productPuid)
        {
            var result = await _service.GetProductByPuid(projectPuid, productPuid);
            return FromServiceResult(result, p => new ProductResponse { Puid = p.Puid, Name = p.Name, Description = p.Description, CreatedAt = p.Created_At, UpdatedAt = p.Last_Updated});
        }
        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpGet]
        public async Task<IActionResult> GetProductsByProjectPuid(string projectPuid)
        {
            var result = await _service.GetProductsByProjectPuid(projectPuid);
            return FromServiceResult(result, projects => projects.Select(p => new ProductResponse { Puid = p.Puid, Name = p.Name, Description = p.Description, CreatedAt = p.Created_At, UpdatedAt = p.Last_Updated}).ToList());
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpPost]
        public async Task<IActionResult> AddProduct(string projectPuid, [FromBody] AddProductRequest req)
        {
            var result = await _service.AddProduct(projectPuid, req.Name, req.Description);
            return FromServiceResult(result, (p) => new ProductResponse { Puid = p.Puid, Name = p.Name, Description = p.Description, CreatedAt = p.Created_At, UpdatedAt = p.Last_Updated});
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpPut("{productPuid}")]
        public async Task<IActionResult> UpdateProduct(string projectPuid, string productPuid, [FromBody] AddProductRequest req)
        {
            var result = await _service.UpdateProduct(projectPuid, productPuid, req.Name, req.Description);
            return FromServiceResult(result, p => new ProductResponse { Puid = p.Puid, Name = p.Name, Description = p.Description, CreatedAt = p.Created_At, UpdatedAt = p.Last_Updated});
        }

        [Authorize(Policy = "IsOwnerOrAdmin")]
        [HttpDelete("{productPuid}")]
        public async Task<IActionResult> DeleteProduct(string projectPuid, string productPuid)
        {
            var result = await _service.DeleteProduct(projectPuid, productPuid);
            return FromServiceResult(result);
        }
    }
}
