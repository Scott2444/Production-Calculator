using System.Diagnostics.CodeAnalysis;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductionCalculator.API.Controllers;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.API.Tests;

[ExcludeFromCodeCoverage]
public class AttributesControllerTests
{
    private static AttributesController CreateController(IAttributeService service)
    {
        var controller = new AttributesController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return controller;
    }

    private static ProjectAttribute CreateAttribute(string puid = "attrPuid", string name = "Attribute")
    {
        return new ProjectAttribute
        {
            Attribute_Id = 1,
            Project_Id = 1,
            Puid = puid,
            Name = name,
            Description = "desc",
            Unit = "u",
            Version = 1,
            Created_At = DateTime.UtcNow,
            Last_Updated = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetAttributeByPuid_ValidRequest_Returns200()
    {
        var service = A.Fake<IAttributeService>();
        var attribute = CreateAttribute();
        A.CallTo(() => service.GetAttributeByPuid("projPuid", "attrPuid")).Returns(ServiceResult<ProjectAttribute>.SuccessResult(attribute));
        var controller = CreateController(service);

        var result = await controller.GetAttributeByPuid("projPuid", "attrPuid");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        Assert.IsType<AttributeResponse>(obj.Value);
    }

    [Fact]
    public async Task AddAttribute_ValidRequest_Returns201()
    {
        var service = A.Fake<IAttributeService>();
        var attribute = CreateAttribute();
        var req = new AttributeRequest { Name = "New", Description = "desc", Unit = "u" };
        A.CallTo(() => service.AddAttribute("projPuid", req.Name, req.Description, req.Unit)).Returns(ServiceResult<ProjectAttribute>.SuccessResult(attribute, ServiceStatus.Created201));
        var controller = CreateController(service);

        var result = await controller.AddAttribute("projPuid", req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, obj.StatusCode);
    }

    [Fact]
    public async Task DeleteAttribute_ValidRequest_Returns204()
    {
        var service = A.Fake<IAttributeService>();
        A.CallTo(() => service.DeleteAttribute("projPuid", "attrPuid")).Returns(ServiceResult.SuccessResult(ServiceStatus.NoContent204));
        var controller = CreateController(service);

        var result = await controller.DeleteAttribute("projPuid", "attrPuid");

        var obj = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(204, obj.StatusCode);
    }

    [Fact]
    public async Task GetAttributesByProjectPuid_ValidRequest_Returns200AndList()
    {
        var service = A.Fake<IAttributeService>();
        var attributes = new List<ProjectAttribute> { CreateAttribute("attrPuid1", "Attr1"), CreateAttribute("attrPuid2", "Attr2") };
        A.CallTo(() => service.GetAttributesByProjectPuid("projPuid")).Returns(ServiceResult<List<ProjectAttribute>>.SuccessResult(attributes));
        var controller = CreateController(service);

        var result = await controller.GetAttributesByProjectPuid("projPuid");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        var list = Assert.IsType<List<AttributeResponse>>(obj.Value);
        Assert.Equal(2, list.Count);
        Assert.Equal("attrPuid1", list[0].Puid);
        Assert.Equal("attrPuid2", list[1].Puid);
    }

    [Fact]
    public async Task UpdateAttribute_ValidRequest_Returns200()
    {
        var service = A.Fake<IAttributeService>();
        var attribute = CreateAttribute("attrPuid", "UpdatedAttr");
        var req = new AttributeRequest { Name = "UpdatedAttr", Description = "desc", Unit = "u" };
        A.CallTo(() => service.UpdateAttribute("projPuid", "attrPuid", req.Name, req.Description, req.Unit)).Returns(ServiceResult<ProjectAttribute>.SuccessResult(attribute));
        var controller = CreateController(service);

        var result = await controller.UpdateAttribute("projPuid", "attrPuid", req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, obj.StatusCode);
        Assert.IsType<AttributeResponse>(obj.Value);
        var response = (AttributeResponse)obj.Value;
        Assert.Equal("attrPuid", response.Puid);
        Assert.Equal("UpdatedAttr", response.Name);
    }
}
