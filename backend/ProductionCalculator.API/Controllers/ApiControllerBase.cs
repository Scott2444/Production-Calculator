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
                return result.Status == ServiceStatus.Created201
                    ? Created(string.Empty, body)
                    : StatusCode((int)result.Status, body);
            }

            return StatusCode((int)result.Status);
        }
    }
}
