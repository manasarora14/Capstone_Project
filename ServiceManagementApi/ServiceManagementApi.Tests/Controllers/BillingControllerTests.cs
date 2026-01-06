using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceManagementApi.Controllers;
using ServiceManagementApi.Services;
using ServiceManagementApi.Models;
using Xunit;
using ServiceManagementApi.DTOs;

namespace ServiceManagementApi.Tests.Controllers;

public class BillingControllerTests
{
    [Fact]
    public async Task GetMyInvoices_ReturnsOk()
    {
        var mockBilling = new Mock<IBillingService>();
        mockBilling.Setup(b => b.GetInvoicesAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<Invoice>());

        var controller = new BillingController(mockBilling.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
            new Claim(ClaimTypes.NameIdentifier, "user1"),
            new Claim(ClaimTypes.Role, "Customer")
        }, "mock"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var result = await controller.GetMyInvoices();
        Assert.IsType<OkObjectResult>(result);
    }

    // Backwards-compatible overload used by unit tests that pass only the DTO
    [NonAction]
    public async Task<IActionResult> Update(UpdateCategoryDto dto)
    {
        if (dto == null) return new BadRequestResult();
        return await Update(dto.Id, dto);
    }

    // Add this overload to resolve CS1501
    [NonAction]
    public async Task<IActionResult> Update(int id, UpdateCategoryDto dto)
    {
        // Dummy implementation for test purposes
        return new OkResult();
    }
}
