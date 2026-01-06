using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceManagementApi.Controllers;
using ServiceManagementApi.DTOs;
using ServiceManagementApi.Services;
using Xunit;

namespace ServiceManagementApi.Tests.Controllers;

public class ServiceRequestControllerTests
{
    [Fact]
    public async Task Respond_AcceptWithPlannedStart_ReturnsOk()
    {
        var mockService = new Mock<IServiceRequestService>();
        mockService.Setup(s => s.RespondToAssignmentAsync(1, "tech1", true, It.IsAny<System.DateTime?>())).ReturnsAsync(true);

        var mockAvailability = new Mock<IAvailabilityService>();

        var controller = new ServiceRequestController(mockService.Object, mockAvailability.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
            new Claim(ClaimTypes.NameIdentifier, "tech1"),
            new Claim(ClaimTypes.Role, "Technician")
        }, "mock"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var dto = new ServiceManagementApi.DTOs.RespondAssignmentDto { RequestId = 1, Accept = true, PlannedStartUtc = System.DateTime.UtcNow };
        var result = await controller.Respond(1, dto);
        Assert.IsType<OkObjectResult>(result);
    }
}
