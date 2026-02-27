using Microsoft.AspNetCore.Mvc;

namespace ProductionCalculator.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthCheckController : ApiControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { status = "Healthy" });
        }
    }
}
