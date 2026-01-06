using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceManagementApi.Controllers;
using ServiceManagementApi.DTOs;
using ServiceManagementApi.Services;
using Xunit;

namespace ServiceManagementApi.Tests.Controllers;

public class AdminControllerTests
{
    private readonly Mock<IAdminService> _mockAdminService;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        _mockAdminService = new Mock<IAdminService>();
        _controller = new AdminController(_mockAdminService.Object);
        
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

    // TC-06: FR-ADMIN-01, FR-ADMIN-03 - Get All Users
    [Fact]
    public async Task GetUsers_ReturnsListOfUsers()
    {
        // Arrange
        var users = new List<object>
        {
            new { Id = "1", Email = "user1@example.com", Role = "Customer" },
            new { Id = "2", Email = "user2@example.com", Role = "Technician" }
        };

        _mockAdminService.Setup(s => s.GetAllUsersWithRolesAsync())
            .ReturnsAsync(users);

        // Act
        var result = await _controller.GetUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultUsers = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
        Assert.Equal(2, resultUsers.Count());
    }

    // TC-07: FR-ADMIN-02 - Update User Role
    [Fact]
    public async Task UpdateRole_WithValidData_ReturnsOk()
    {
        // Arrange
        var updateRoleDto = new UpdateRoleDto
        {
            UserId = "user1",
            NewRole = "Technician"
        };

        var identityResult = IdentityResult.Success;
        _mockAdminService.Setup(s => s.UpdateUserRoleAsync(updateRoleDto.UserId, updateRoleDto.NewRole))
            .ReturnsAsync(identityResult);

        // Act
        var result = await _controller.UpdateRole(updateRoleDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockAdminService.Verify(s => s.UpdateUserRoleAsync("user1", "Technician"), Times.Once);
    }

    // TC-08: FR-ADMIN-02 - Update User Role with Invalid User
    [Fact]
    public async Task UpdateRole_WithInvalidUserId_ReturnsBadRequest()
    {
        // Arrange
        var updateRoleDto = new UpdateRoleDto
        {
            UserId = "invalid-user",
            NewRole = "Technician"
        };

        var identityResult = IdentityResult.Failed(new IdentityError 
        { 
            Description = "User not found" 
        });
        
        _mockAdminService.Setup(s => s.UpdateUserRoleAsync(updateRoleDto.UserId, updateRoleDto.NewRole))
            .ReturnsAsync(identityResult);

        // Act
        var result = await _controller.UpdateRole(updateRoleDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }
}

