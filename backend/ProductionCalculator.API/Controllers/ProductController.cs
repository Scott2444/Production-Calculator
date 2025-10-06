using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _service;

        public ProductsController(IProductService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var products = await _service.GetAllAsync();
            return Ok(products);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var product = await _service.GetByIdAsync(id);
            return product is null ? NotFound() : Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Product product)
        {
            var created = await _service.CreateAsync(product);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
    }
}
