using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceManagementApi.Controllers;
using ServiceManagementApi.DTOs;
using ServiceManagementApi.Models;
using ServiceManagementApi.Services;
using Xunit;

namespace ServiceManagementApi.Tests.Controllers;

public class CategoryControllerExtendedTests
{
    private readonly Mock<ICategoryService> _mockCategoryService;
    private readonly CategoryController _controller;

    public CategoryControllerExtendedTests()
    {
        _mockCategoryService = new Mock<ICategoryService>();
        _controller = new CategoryController(_mockCategoryService.Object);
        
        // Setup admin user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    // TC-09: FR-CAT-01 - Create Service Category
    [Fact]
    public async Task Create_WithValidData_ReturnsCreated()
    {
        // Arrange
        var createDto = new CreateCategoryDto
        {
            Name = "Installation",
            Description = "Installation services",
            BaseCharge = 500.00m,
            SlaHours = 24
        };

        var createdCategory = new ServiceCategory
        {
            Id = 1,
            Name = createDto.Name,
            Description = createDto.Description,
            BaseCharge = createDto.BaseCharge,
            SlaHours = createDto.SlaHours
        };

        _mockCategoryService.Setup(s => s.CreateAsync(createDto))
            .ReturnsAsync(createdCategory);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        _mockCategoryService.Verify(s => s.CreateAsync(createDto), Times.Once);
    }

    // TC-10: FR-CAT-02 - Define Standard Service Charges
    [Fact]
    public async Task Create_SetsCorrectBaseCharge_ReturnsCreated()
    {
        // Arrange
        var createDto = new CreateCategoryDto
        {
            Name = "Maintenance",
            Description = "Maintenance services",
            BaseCharge = 300.00m,
            SlaHours = 36
        };

        var createdCategory = new ServiceCategory
        {
            Id = 1,
            Name = createDto.Name,
            BaseCharge = createDto.BaseCharge,
            SlaHours = createDto.SlaHours
        };

        _mockCategoryService.Setup(s => s.CreateAsync(createDto))
            .ReturnsAsync(createdCategory);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result);
        _mockCategoryService.Verify(s => s.CreateAsync(It.Is<CreateCategoryDto>(
            dto => dto.BaseCharge == 300.00m)), Times.Once);
    }

    // TC-11: FR-CAT-03 - Define SLA Time for Service
    [Fact]
    public async Task Create_SetsCorrectSlaHours_ReturnsCreated()
    {
        // Arrange
        var createDto = new CreateCategoryDto
        {
            Name = "Repair",
            Description = "Repair services",
            BaseCharge = 400.00m,
            SlaHours = 12
        };

        var createdCategory = new ServiceCategory
        {
            Id = 1,
            Name = createDto.Name,
            BaseCharge = createDto.BaseCharge,
            SlaHours = createDto.SlaHours
        };

        _mockCategoryService.Setup(s => s.CreateAsync(createDto))
            .ReturnsAsync(createdCategory);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result);
        _mockCategoryService.Verify(s => s.CreateAsync(It.Is<CreateCategoryDto>(
            dto => dto.SlaHours == 12)), Times.Once);
    }

    // TC-12: FR-CAT-04 - Update Service Category
    [Fact]
    public async Task Update_WithValidData_ReturnsOk()
    {
        // Arrange
        var updateDto = new UpdateCategoryDto
        {
            Id = 1,
            Name = "Updated Installation",
            Description = "Updated description",
            BaseCharge = 600.00m,
            SlaHours = 48
        };

        _mockCategoryService.Setup(s => s.UpdateAsync(updateDto))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Update(updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockCategoryService.Verify(s => s.UpdateAsync(updateDto), Times.Once);
    }

    // TC-13: FR-CAT-04 - Delete Service Category
    [Fact]
    public async Task Delete_WithValidId_ReturnsOk()
    {
        // Arrange
        var categoryId = 1;
        _mockCategoryService.Setup(s => s.DeleteAsync(categoryId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(categoryId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockCategoryService.Verify(s => s.DeleteAsync(categoryId), Times.Once);
    }
}

