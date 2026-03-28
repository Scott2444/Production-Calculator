using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.API.Controllers
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected IActionResult FromServiceResult<T, TResponse>(ServiceResult<T> result, Func<T, TResponse> map)
        {
            if (result.Redirect && !string.IsNullOrEmpty(result.Location))
            {
                Response.Headers["Location"] = result.Location;
                return StatusCode((int)result.Status);
            }
            if (result.Success && result.Data != null)
            {
                var body = map(result.Data);
                return StatusCode((int)result.Status, body);
            }
            return StatusCode((int)result.Status, new { error = result.ErrorMessage });
        }

        protected IActionResult FromServiceResult(ServiceResult result)
        {
            if (result.Redirect && !string.IsNullOrEmpty(result.Location))
            {
                Response.Headers["Location"] = result.Location;
                return StatusCode((int)result.Status);
            }
            if (result.Success) 
            {
                return StatusCode((int)result.Status); 
            }
            return StatusCode((int)result.Status, new { error = result.ErrorMessage });
        }

        protected IActionResult FromServiceResult<T>(ServiceResult<T> result)
        {
            if (result.Redirect && !string.IsNullOrEmpty(result.Location))
            {
                Response.Headers["Location"] = result.Location;
                return StatusCode((int)result.Status);
            }
            if (result.Success && result.Data != null)
            {
                return StatusCode((int)result.Status, result.Data);
            }
            return StatusCode((int)result.Status, new { error = result.ErrorMessage });
        }
    }
}
