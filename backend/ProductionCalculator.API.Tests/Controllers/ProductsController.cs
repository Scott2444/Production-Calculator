using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.API.Controllers;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.API.Tests;

public class ProductsControllerTests
{
    private static ProductsController CreateController(IProductService service)
    {
        var controller = new ProductsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return controller;
    }

    private static Product CreateProduct(string puid = "prodPuid", string name = "Product")
    {
        return new Product
        {
            Product_Id = 1,
            Project_Id = 1,
            Puid = puid,
            Name = name,
            Description = "desc",
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetProductByPuid_ValidRequest_Returns200OkWithResponse()
    {
        var service = A.Fake<IProductService>();
        var product = CreateProduct();
        A.CallTo(() => service.GetProductByPuid("projPuid", "prodPuid")).Returns(ServiceResult<Product>.SuccessResult(product));
        var controller = CreateController(service);

        var result = await controller.GetProductByPuid("projPuid", "prodPuid");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        var response = Assert.IsType<ProductResponse>(obj.Value);
        Assert.Equal("prodPuid", response.Puid);
    }

    [Fact]
    public async Task GetProductByPuid_ProductNotFound_Returns404NotFound()
    {
        var service = A.Fake<IProductService>();
        A.CallTo(() => service.GetProductByPuid("projPuid", "missing")).Returns(ServiceResult<Product>.Fail(ServiceStatus.NotFound404, "Not Found"));
        var controller = CreateController(service);

        var result = await controller.GetProductByPuid("projPuid", "missing");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, obj.StatusCode);
    }

    [Fact]
    public async Task GetProductByPuid_ServiceRedirect_Returns303SeeOther()
    {
        var service = A.Fake<IProductService>();
        A.CallTo(() => service.GetProductByPuid("alias", "prodPuid"))
            .Returns(ServiceResult<Product>.Redirection(ServiceStatus.SeeOther303, "/projects/canonical/products/prodPuid"));
        var controller = CreateController(service);

        var result = await controller.GetProductByPuid("alias", "prodPuid");

        var obj = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(303, obj.StatusCode);
        Assert.Equal("/projects/canonical/products/prodPuid", controller.Response.Headers["Location"]);
    }

    [Fact]
    public async Task GetProductsByProjectPuid_ValidRequest_Returns200OkWithList()
    {
        var service = A.Fake<IProductService>();
        var products = new List<Product> { CreateProduct(puid: "p1"), CreateProduct(puid: "p2") };
        A.CallTo(() => service.GetProductsByProjectPuid("projPuid")).Returns(ServiceResult<List<Product>>.SuccessResult(products));
        var controller = CreateController(service);

        var result = await controller.GetProductsByProjectPuid("projPuid");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        var response = Assert.IsType<List<ProductResponse>>(obj.Value);
        Assert.Equal(2, response.Count);
    }

    [Fact]
    public async Task GetProductsByProjectPuid_ProjectNotFound_Returns404NotFound()
    {
        var service = A.Fake<IProductService>();
        A.CallTo(() => service.GetProductsByProjectPuid("missing")).Returns(ServiceResult<List<Product>>.Fail(ServiceStatus.NotFound404, "Not Found"));
        var controller = CreateController(service);

        var result = await controller.GetProductsByProjectPuid("missing");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, obj.StatusCode);
    }

    [Fact]
    public async Task GetProductsByProjectPuid_ServiceRedirect_Returns303SeeOther()
    {
        var service = A.Fake<IProductService>();
        A.CallTo(() => service.GetProductsByProjectPuid("alias"))
            .Returns(ServiceResult<List<Product>>.Redirection(ServiceStatus.SeeOther303, "/projects/canonical/products"));
        var controller = CreateController(service);

        var result = await controller.GetProductsByProjectPuid("alias");

        var obj = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(303, obj.StatusCode);
        Assert.Equal("/projects/canonical/products", controller.Response.Headers["Location"]);
    }

    [Fact]
    public async Task AddProduct_ValidRequest_Returns201Created()
    {
        var service = A.Fake<IProductService>();
        var product = CreateProduct();
        A.CallTo(() => service.AddProduct("projPuid", "Prod", "desc")).Returns(ServiceResult<Product>.SuccessResult(product, ServiceStatus.Created201));
        var controller = CreateController(service);
        var request = new ProductRequest { Name = "Prod", Description = "desc" };

        var result = await controller.AddProduct("projPuid", request);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, obj.StatusCode);
        var response = Assert.IsType<ProductResponse>(obj.Value);
        Assert.Equal("prodPuid", response.Puid);
    }

    [Fact]
    public async Task AddProduct_ServiceReturnsError_ReturnsErrorStatus()
    {
        var service = A.Fake<IProductService>();
        A.CallTo(() => service.AddProduct("projPuid", "Prod", "desc")).Returns(ServiceResult<Product>.Fail(ServiceStatus.Conflict409, "Conflict"));
        var controller = CreateController(service);
        var request = new ProductRequest { Name = "Prod", Description = "desc" };

        var result = await controller.AddProduct("projPuid", request);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(409, obj.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_ValidRequest_Returns200OkWithResponse()
    {
        var service = A.Fake<IProductService>();
        var product = CreateProduct();
        A.CallTo(() => service.UpdateProduct("projPuid", "prodPuid", "New", "desc")).Returns(ServiceResult<Product>.SuccessResult(product));
        var controller = CreateController(service);
        var request = new ProductRequest { Name = "New", Description = "desc" };

        var result = await controller.UpdateProduct("projPuid", "prodPuid", request);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_ServiceReturnsError_ReturnsErrorStatus()
    {
        var service = A.Fake<IProductService>();
        A.CallTo(() => service.UpdateProduct("projPuid", "prodPuid", "New", "desc")).Returns(ServiceResult<Product>.Fail(ServiceStatus.NotFound404, "Not Found"));
        var controller = CreateController(service);
        var request = new ProductRequest { Name = "New", Description = "desc" };

        var result = await controller.UpdateProduct("projPuid", "prodPuid", request);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, obj.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_ValidRequest_Returns204NoContent()
    {
        var service = A.Fake<IProductService>();
        A.CallTo(() => service.DeleteProduct("projPuid", "prodPuid")).Returns(ServiceResult.SuccessResult(ServiceStatus.NoContent204));
        var controller = CreateController(service);

        var result = await controller.DeleteProduct("projPuid", "prodPuid");

        var obj = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(204, obj.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_ServiceReturnsError_ReturnsErrorStatus()
    {
        var service = A.Fake<IProductService>();
        A.CallTo(() => service.DeleteProduct("projPuid", "prodPuid")).Returns(ServiceResult.Fail(ServiceStatus.NotFound404, "Not Found"));
        var controller = CreateController(service);

        var result = await controller.DeleteProduct("projPuid", "prodPuid");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, obj.StatusCode);
    }
}
