using System.Diagnostics.CodeAnalysis;
using ProductionCalculator.API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace ProductionCalculator.API.Tests.Controllers;

[ExcludeFromCodeCoverage]
public class HealthCheckControllerTests
{
    [Fact]
    public void Get_ReturnsHealthyStatus()
    {
        var controller = new HealthCheckController();
        var result = controller.Get();

        // Assert that the result is OkObjectResult
        var okResult = Assert.IsType<OkObjectResult>(result);
        // Assert that the value is an anonymous object with status = "Healthy"
        var value = okResult.Value;
        Assert.NotNull(value);
        var statusProperty = value.GetType().GetProperty("status");
        Assert.NotNull(statusProperty);
        var statusValue = statusProperty.GetValue(value) as string;
        Assert.Equal("Healthy", statusValue);
    }
}