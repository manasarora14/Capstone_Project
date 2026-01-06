using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceManagementApi.Controllers;
using ServiceManagementApi.DTOs;
using ServiceManagementApi.Services;
using Xunit;

namespace ServiceManagementApi.Tests.Controllers;

public class CategoryControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsOkWithCategories()
    {
        // Arrange
        var mockService = new Mock<ICategoryService>();
        mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<ServiceManagementApi.Models.ServiceCategory>
        {
            new ServiceManagementApi.Models.ServiceCategory { Id = 1, Name = "Installation", Description = "d", BaseCharge = 100, SlaHours = 4 }
        });

        var controller = new CategoryController(mockService.Object);

        // Act
        var result = await controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = Assert.IsAssignableFrom<IEnumerable<ServiceManagementApi.Models.ServiceCategory>>(okResult.Value);
        Assert.Single(value);
    }
}
