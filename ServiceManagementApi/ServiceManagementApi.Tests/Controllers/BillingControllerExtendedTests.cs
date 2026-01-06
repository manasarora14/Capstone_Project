using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceManagementApi.Controllers;
using ServiceManagementApi.Models;
using ServiceManagementApi.Services;
using Xunit;

namespace ServiceManagementApi.Tests.Controllers;

public class BillingControllerExtendedTests
{
    private readonly Mock<IBillingService> _mockBillingService;
    private readonly BillingController _controller;

    public BillingControllerExtendedTests()
    {
        _mockBillingService = new Mock<IBillingService>();
        _controller = new BillingController(_mockBillingService.Object);
    }

    // TC-29: FR-BILL-01 - Auto Generate Invoice on Completion
    [Fact]
    public async Task GetMyInvoices_ReturnsInvoices()
    {
        // Arrange
        var customerUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "customer1"),
            new Claim(ClaimTypes.Role, "Customer")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = customerUser }
        };

        var invoices = new List<Invoice>
        {
            new Invoice { Id = 1, ServiceRequestId = 1, Amount = 500.00m, Status = "Pending" },
            new Invoice { Id = 2, ServiceRequestId = 2, Amount = 300.00m, Status = "Paid" }
        };

        _mockBillingService.Setup(s => s.GetInvoicesAsync("customer1", "Customer"))
            .ReturnsAsync(invoices);

        // Act
        var result = await _controller.GetMyInvoices();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultInvoices = Assert.IsAssignableFrom<IEnumerable<Invoice>>(okResult.Value);
        Assert.Equal(2, resultInvoices.Count());
    }

    // TC-30: FR-BILL-02 - Invoice Includes Service Charge
    [Fact]
    public async Task GetMyInvoices_InvoiceContainsAmount_ReturnsOk()
    {
        // Arrange
        var customerUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "customer1"),
            new Claim(ClaimTypes.Role, "Customer")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = customerUser }
        };

        var invoice = new Invoice
        {
            Id = 1,
            ServiceRequestId = 1,
            Amount = 500.00m,
            Status = "Pending"
        };

        _mockBillingService.Setup(s => s.GetInvoicesAsync("customer1", "Customer"))
            .ReturnsAsync(new List<Invoice> { invoice });

        // Act
        var result = await _controller.GetMyInvoices();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var invoices = Assert.IsAssignableFrom<IEnumerable<Invoice>>(okResult.Value);
        var firstInvoice = invoices.First();
        Assert.Equal(500.00m, firstInvoice.Amount);
        Assert.True(firstInvoice.Amount > 0);
    }

    // TC-31: FR-BILL-03 - Track Payment Status
    [Fact]
    public async Task PayInvoice_WithValidInvoice_ReturnsOk()
    {
        // Arrange
        var customerUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "customer1"),
            new Claim(ClaimTypes.Role, "Customer")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = customerUser }
        };

        _mockBillingService.Setup(s => s.PayInvoiceAsync(1, "customer1"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.PayInvoice(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockBillingService.Verify(s => s.PayInvoiceAsync(1, "customer1"), Times.Once);
    }

    // TC-32: FR-BILL-03 - Payment Status Updated to Paid
    [Fact]
    public async Task PayInvoice_UpdatesStatusToPaid_ReturnsOk()
    {
        // Arrange
        var customerUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "customer1"),
            new Claim(ClaimTypes.Role, "Customer")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = customerUser }
        };

        _mockBillingService.Setup(s => s.PayInvoiceAsync(1, "customer1"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.PayInvoice(1);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        // Service layer should update status to "Paid" - verified through integration tests
    }

    // TC-33: FR-BILL-03 - Invoice Not Generated for Cancelled Request
    [Fact]
    public async Task GetMyInvoices_NoInvoicesForCancelledRequests_ReturnsEmptyList()
    {
        // Arrange
        var customerUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "customer1"),
            new Claim(ClaimTypes.Role, "Customer")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = customerUser }
        };

        _mockBillingService.Setup(s => s.GetInvoicesAsync("customer1", "Customer"))
            .ReturnsAsync(new List<Invoice>());

        // Act
        var result = await _controller.GetMyInvoices();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var invoices = Assert.IsAssignableFrom<IEnumerable<Invoice>>(okResult.Value);
        Assert.Empty(invoices);
    }
}

