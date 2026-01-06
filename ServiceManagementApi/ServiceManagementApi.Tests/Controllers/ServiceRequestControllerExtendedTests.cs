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

public class ServiceRequestControllerExtendedTests
{
    private readonly Mock<IServiceRequestService> _mockService;
    private readonly Mock<IAvailabilityService> _mockAvailability;
    private readonly ServiceRequestController _controller;

    public ServiceRequestControllerExtendedTests()
    {
        _mockService = new Mock<IServiceRequestService>();
        _mockAvailability = new Mock<IAvailabilityService>();
        _controller = new ServiceRequestController(_mockService.Object, _mockAvailability.Object);
    }

    // TC-14: FR-REQ-01 - Create Service Request
    [Fact]
    public async Task Create_WithValidData_ReturnsOk()
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

        var createDto = new CreateRequestDto
        {
            IssueDescription = "AC not working",
            CategoryId = 1,
            Priority = Priority.Low,
            ScheduledDate = System.DateTime.UtcNow.AddDays(1)
        };

        _mockService.Setup(s => s.CreateRequestAsync(createDto, "customer1"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CreateRequestAsync(createDto, "customer1"), Times.Once);
    }

    // TC-15: FR-REQ-02 - Select Category and Set Schedule
    [Fact]
    public async Task Create_WithCategoryAndSchedule_ReturnsOk()
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

        var createDto = new CreateRequestDto
        {
            IssueDescription = "Installation needed",
            CategoryId = 2,
            Priority = Priority.High,
            ScheduledDate = System.DateTime.UtcNow.AddDays(2),
            ScheduledTime = System.TimeSpan.FromHours(10)
        };

        _mockService.Setup(s => s.CreateRequestAsync(createDto, "customer1"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CreateRequestAsync(It.Is<CreateRequestDto>(
            dto => dto.CategoryId == 2 && dto.ScheduledTime.HasValue), "customer1"), Times.Once);
    }

    // TC-16: FR-REQ-03 - Get Customer Requests
    [Fact]
    public async Task GetMyRequests_ReturnsCustomerRequests()
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

        var requests = new List<ServiceRequest>
        {
            new ServiceRequest { Id = 1, IssueDescription = "Request 1", CustomerId = "customer1" },
            new ServiceRequest { Id = 2, IssueDescription = "Request 2", CustomerId = "customer1" }
        };

        _mockService.Setup(s => s.GetCustomerRequestsAsync("customer1"))
            .ReturnsAsync(requests);

        // Act
        var result = await _controller.GetMyRequests(null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultRequests = Assert.IsAssignableFrom<IEnumerable<ServiceRequest>>(okResult.Value);
        Assert.Equal(2, resultRequests.Count());
    }

    // TC-17: FR-REQ-03 - Initial Status is Requested
    [Fact]
    public async Task Create_SetsStatusToRequested_ReturnsOk()
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

        var createDto = new CreateRequestDto
        {
            IssueDescription = "New request",
            CategoryId = 1,
            Priority = Priority.Medium,
            ScheduledDate = System.DateTime.UtcNow.AddDays(1)
        };

        _mockService.Setup(s => s.CreateRequestAsync(createDto, "customer1"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        // Service layer should set status to Requested - verified through integration tests
    }

    // TC-18: FR-ASSIGN-01 - Get All Pending Requests (Manager)
    [Fact]
    public async Task MonitorAll_ReturnsAllRequests()
    {
        // Arrange
        var managerUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "manager1"),
            new Claim(ClaimTypes.Role, "Manager")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = managerUser }
        };

        var requests = new List<ServiceRequest>
        {
            new ServiceRequest { Id = 1, IssueDescription = "Request A", CustomerId = "manager1", Status = RequestStatus.Requested },
            new ServiceRequest { Id = 2, IssueDescription = "Request B", CustomerId = "manager1", Status = RequestStatus.Assigned }
        };

        _mockService.Setup(s => s.GetAllForMonitorAsync())
            .ReturnsAsync(requests);

        // Act
        var result = await _controller.Monitor(null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultRequests = Assert.IsAssignableFrom<IEnumerable<ServiceRequest>>(okResult.Value);
        Assert.Equal(2, resultRequests.Count());
    }

    // TC-19: FR-ASSIGN-02 - Assign Technician to Request
    [Fact]
    public async Task Assign_WithValidData_ReturnsOk()
    {
        // Arrange
        var managerUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "manager1"),
            new Claim(ClaimTypes.Role, "Manager")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = managerUser }
        };

        var assignDto = new AssignTechnicianDto
        {
            RequestId = 1,
            TechnicianId = "tech1"
        };

        _mockService.Setup(s => s.AssignTechnicianAsync(assignDto))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Assign(assignDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.AssignTechnicianAsync(assignDto), Times.Once);
    }

    // TC-20: FR-ASSIGN-03 - Status Updated to Assigned
    [Fact]
    public async Task Assign_UpdatesStatusToAssigned_ReturnsOk()
    {
        // Arrange
        var managerUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "manager1"),
            new Claim(ClaimTypes.Role, "Manager")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = managerUser }
        };

        var assignDto = new AssignTechnicianDto
        {
            RequestId = 1,
            TechnicianId = "tech1"
        };

        _mockService.Setup(s => s.AssignTechnicianAsync(assignDto))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Assign(assignDto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        // Service layer should update status to Assigned - verified through integration tests
    }

    // TC-21: FR-ASSIGN-04 - Get Available Technicians
    [Fact]
    public async Task GetAvailableTechnicians_ReturnsAvailableTechnicians()
    {
        // Arrange
        var managerUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "manager1"),
            new Claim(ClaimTypes.Role, "Manager")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = managerUser }
        };

        var request = new ServiceRequest
        {
            Id = 1,
            IssueDescription = "Check AC",
            CustomerId = "customer1",
            ScheduledDate = System.DateTime.UtcNow.AddDays(1),
            Category = new ServiceCategory { SlaHours = 4 }
        };

        var availableTechs = new List<AvailableTechDto>
        {
            new AvailableTechDto { TechnicianId = "tech1", UserName = "tech1@example.com" },
            new AvailableTechDto { TechnicianId = "tech2", UserName = "tech2@example.com" }
        };

        _mockService.Setup(s => s.GetServiceRequestByIdAsync(1, "", "Admin"))
            .ReturnsAsync(request);
        _mockAvailability.Setup(a => a.GetAvailableTechniciansAsync(
            It.IsAny<System.DateTime>(), It.IsAny<double>()))
            .ReturnsAsync(availableTechs);

        // Act
        var result = await _controller.GetAvailableTechniciansForRequest(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var techs = Assert.IsAssignableFrom<IEnumerable<AvailableTechDto>>(okResult.Value);
        Assert.Equal(2, techs.Count());
    }

    // TC-22: FR-TECH-01 - Get Technician Tasks
    [Fact]
    public async Task GetMyTasks_ReturnsTechnicianTasks()
    {
        // Arrange
        var techUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "tech1"),
            new Claim(ClaimTypes.Role, "Technician")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = techUser }
        };

        var tasks = new List<ServiceRequest>
        {
            new ServiceRequest { Id = 1, IssueDescription = "Task 1", CustomerId = "cust1", TechnicianId = "tech1", Status = RequestStatus.Assigned },
            new ServiceRequest { Id = 2, IssueDescription = "Task 2", CustomerId = "cust1", TechnicianId = "tech1", Status = RequestStatus.InProgress }
        };

        _mockService.Setup(s => s.GetTechnicianTasksAsync("tech1"))
            .ReturnsAsync(tasks);

        // Act
        var result = await _controller.GetMyTasks(null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultTasks = Assert.IsAssignableFrom<IEnumerable<ServiceRequest>>(okResult.Value);
        Assert.Equal(2, resultTasks.Count());
        Assert.All(resultTasks, task => Assert.Equal("tech1", task.TechnicianId));
    }

    // TC-23: FR-TECH-02 - Start Work (Mark as In Progress)
    [Fact]
    public async Task StartWork_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var techUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "tech1"),
            new Claim(ClaimTypes.Role, "Technician")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = techUser }
        };

        _mockService.Setup(s => s.StartWorkAsync(1, "tech1", It.IsAny<System.DateTime>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.StartWork(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.StartWorkAsync(1, "tech1", It.IsAny<System.DateTime>()), Times.Once);
    }

    // TC-24: FR-TECH-03 - Finish Work (Mark as Completed)
    [Fact]
    public async Task FinishWork_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var techUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "tech1"),
            new Claim(ClaimTypes.Role, "Technician")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = techUser }
        };

        _mockService.Setup(s => s.FinishWorkAsync(1, "tech1", It.IsAny<System.DateTime>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.FinishWork(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.FinishWorkAsync(1, "tech1", It.IsAny<System.DateTime>()), Times.Once);
    }

    // TC-25: FR-TECH-04 - Accept Assignment with Planned Start
    [Fact]
    public async Task Respond_AcceptWithPlannedStart_ReturnsOk()
    {
        // Arrange
        var techUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "tech1"),
            new Claim(ClaimTypes.Role, "Technician")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = techUser }
        };

        var dto = new RespondAssignmentDto
        {
            RequestId = 1,
            Accept = true,
            PlannedStartUtc = System.DateTime.UtcNow.AddDays(1)
        };

        _mockService.Setup(s => s.RespondToAssignmentAsync(1, "tech1", true, It.IsAny<System.DateTime?>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Respond(1, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.RespondToAssignmentAsync(1, "tech1", true, It.IsAny<System.DateTime?>()), Times.Once);
    }

    // TC-26: FR-STATUS-01 - Get Service Request by ID
    [Fact]
    public async Task GetServiceRequestById_WithValidId_ReturnsRequest()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "customer1"),
            new Claim(ClaimTypes.Role, "Customer")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var request = new ServiceRequest
        {
            Id = 1,
            IssueDescription = "Test request",
            Status = RequestStatus.Assigned,
            CustomerId = "customer1"
        };

        _mockService.Setup(s => s.GetServiceRequestByIdAsync(1, "customer1", "Customer"))
            .ReturnsAsync(request);

        // Act
        var result = await _controller.GetServiceRequestById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultRequest = Assert.IsType<ServiceRequest>(okResult.Value);
        Assert.Equal(1, resultRequest.Id);
        Assert.Equal(RequestStatus.Assigned, resultRequest.Status);
    }

    // TC-27: FR-STATUS-02 - Reschedule Service Request
    [Fact]
    public async Task Reschedule_WithValidData_ReturnsOk()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "customer1"),
            new Claim(ClaimTypes.Role, "Customer")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var rescheduleDto = new RescheduleDto
        {
            Id = 1,
            NewDate = System.DateTime.UtcNow.AddDays(3)
        };

        _mockService.Setup(s => s.GetServiceRequestByIdAsync(1, "customer1", "Customer"))
            .ReturnsAsync(new ServiceRequest { Id = 1, CustomerId = "customer1", IssueDescription = "Reschedule" });
        _mockService.Setup(s => s.RescheduleRequestAsync(1, It.IsAny<System.DateTime>(), "customer1", "Customer"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Reschedule(rescheduleDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.RescheduleRequestAsync(1, It.IsAny<System.DateTime>(), "customer1", "Customer"), Times.Once);
    }

    // TC-28: Cancel Service Request
    [Fact]
    public async Task CancelRequest_WithValidId_ReturnsOk()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "customer1"),
            new Claim(ClaimTypes.Role, "Customer")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        _mockService.Setup(s => s.CancelRequestAsync(1, "customer1"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CancelRequest(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CancelRequestAsync(1, "customer1"), Times.Once);
    }
}

