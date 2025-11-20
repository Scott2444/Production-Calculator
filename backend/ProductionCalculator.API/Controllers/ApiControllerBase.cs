using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.API.Controllers
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected IActionResult FromServiceResult<T, TResponse>(ServiceResult<T> result, Func<T, TResponse> map)
        {
            if (result.Success && result.Data != null)
            {
                var body = map(result.Data);
                return StatusCode((int)result.Status, body);
            }

            return StatusCode((int)result.Status, new { error = result.ErrorMessage });
        }

        protected IActionResult FromServiceResult(ServiceResult result)
        {
            if (result.Success) 
            {
                return StatusCode((int)result.Status); 
            }

            return StatusCode((int)result.Status, new { error = result.ErrorMessage });
        }
    }
}
