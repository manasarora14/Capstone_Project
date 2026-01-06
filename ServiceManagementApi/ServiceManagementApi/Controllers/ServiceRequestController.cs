using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceManagementApi.DTOs;
using ServiceManagementApi.Models;
using ServiceManagementApi.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using static ServiceManagementApi.DTOs.TechnicianWorkloadDto;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ServiceRequestController : ControllerBase
{
    private readonly IServiceRequestService _service;
    private readonly IAvailabilityService _availabilityService;
    private readonly ICategoryService? _categoryService;

    public ServiceRequestController(IServiceRequestService service, IAvailabilityService availabilityService, ICategoryService? categoryService = null)
    {
        _service = service;
        _availabilityService = availabilityService;
        _categoryService = categoryService;
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories() => Ok(await _service.GetCategoriesAsync());

    [HttpPost("create")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Create(CreateRequestDto dto)
    {
        await _service.CreateRequestAsync(dto, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(new { message = "Success" });
    }

    [HttpPut("update-status")]
    public async Task<IActionResult> UpdateStatus(UpdateStatusDto dto)
    {
        var success = await _service.UpdateStatusWithBillingAsync(dto);
        return success ? Ok(new { message = "Status updated" }) : NotFound();
    }

    [HttpGet("monitor-all")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Monitor([FromQuery] QueryParameters query)
    {
        var result = await _service.GetAllForMonitorAsync(query);
        return Ok(new
        {
            items = result.Items.Select(MapToDto),
            totalCount = result.TotalCount
        });
    }

    [HttpPut("assign")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Assign(AssignTechnicianDto dto)
    {
        var success = await _service.AssignTechnicianAsync(dto);
        if (!success)
        {
            return BadRequest(new { message = "Technician is unavailable or request not found." });
        }
        return Ok(new { message = "Technician assigned successfully" });
    }

    [HttpGet("available-technicians/{requestId}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> GetAvailableTechniciansForRequest(int requestId)
    {
        var request = await _service.GetServiceRequestByIdAsync(requestId, "", "Admin");
        if (request == null) return NotFound();

        var scheduledStart = request.PlannedStartUtc ?? request.ScheduledDate;
        var duration = request.Category?.SlaHours ?? 0;

        if (duration <= 0)
            return BadRequest(new { message = "Valid category SLA duration required" });

        var availableTechs = await _availabilityService.GetAvailableTechniciansAsync(scheduledStart, duration);
        return Ok(availableTechs);
    }
    [Authorize(Roles = "Technician")]
    [HttpGet("my-tasks")]
    public async Task<IActionResult> GetMyTasks([FromQuery] QueryParameters query)
    {
        var techId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _service.GetTechnicianTasksAsync(techId!, query);
        return Ok(new
        {
            items = result.Items.Select(MapToDto),
            totalCount = result.TotalCount
        });
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("my-requests")]
    public async Task<IActionResult> GetMyRequests([FromQuery] QueryParameters query)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _service.GetCustomerRequestsAsync(userId!, query);
        return Ok(new
        {
            items = result.Items.Select(MapToDto),
            totalCount = result.TotalCount
        });
    }

    [HttpGet("dashboard-stats")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetStats() => Ok(await _service.GetDashboardStatsAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetServiceRequestById(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        var request = await _service.GetServiceRequestByIdAsync(id, userId!, userRole);
      
        return request == null ? NotFound() : Ok(MapToDto(request));
    }

    [Authorize(Roles = "Technician")]
    [HttpPost("{id}/respond")]
    public async Task<IActionResult> Respond(int id, [FromBody] RespondAssignmentDto dto)
    {
        var techId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _service.RespondToAssignmentAsync(id, techId!, dto.Accept, dto.PlannedStartUtc);

        return result ? Ok(new { message = dto.Accept ? "Accepted" : "Rejected" }) : BadRequest("Response failed.");
    }

    [HttpPut("reschedule")]
    public async Task<IActionResult> Reschedule([FromBody] RescheduleDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        var success = await _service.RescheduleRequestAsync(dto.Id, dto.NewDate, userId!, userRole);
        return success ? Ok(new { message = "Rescheduled successfully" }) : Conflict(new { message = "Conflict or unauthorized" });
    }

    [HttpPut("reschedule-by-parts")]
    public async Task<IActionResult> RescheduleByParts([FromBody] RescheduleByPartsDto dto)
    {
        if (!DateTime.TryParse(dto.Date, out var d)) d = DateTime.Today;
        if (!TimeSpan.TryParse(dto.Time, out var t)) t = TimeSpan.Zero;

        var utcDate = DateTime.SpecifyKind(d.Date + t, DateTimeKind.Local).ToUniversalTime();

        var success = await _service.RescheduleRequestAsync(dto.Id, utcDate,
            User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            User.FindFirstValue(ClaimTypes.Role)!);

        return success ? Ok() : Conflict();
    }

    [HttpDelete("{id}/cancel")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> CancelRequest(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _service.CancelRequestAsync(id, userId!);
        return result ? Ok(new { message = "Cancelled" }) : BadRequest("Cannot cancel active work.");
    }

    [Authorize(Roles = "Technician")]
    [HttpPost("{id}/start")]
    public async Task<IActionResult> StartWork(int id)
    {
        var ok = await _service.StartWorkAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!, DateTime.UtcNow);
        return ok ? Ok() : BadRequest();
    }

    [Authorize(Roles = "Technician")]
    [HttpPost("{id}/finish")]
    public async Task<IActionResult> FinishWork(int id, [FromBody] FinishWorkDto dto)
    {
        var techId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        // Pass the notes from the DTO to the service
        var ok = await _service.FinishWorkAsync(id, techId, DateTime.UtcNow, dto.Notes);
        return ok ? Ok(new { message = "Work finished", notes = dto.Notes }) : BadRequest();
    }


    private ServiceRequestResponseDto MapToDto(ServiceRequest r)
    {
        
        var dto = new ServiceRequestResponseDto
        {
            Id = r.Id,
            Status = r.Status,
            StatusName = r.Status.ToString(),
            CustomerId = r.CustomerId ?? string.Empty,
            Priority = r.Priority,
            CustomerName = r.Customer?.FullName ?? r.Customer?.UserName ?? "Unknown",
            TechnicianId = r.TechnicianId,
            TechnicianName = r.Technician?.FullName ?? r.Technician?.UserName ?? "Not Assigned",
            IssueDescription = r.IssueDescription,
            ScheduledDate = r.ScheduledDate,
            TotalPrice = r.TotalPrice,
            CompletedAt = r.CompletedAt,
            WorkStartedAt = r.WorkStartedAt,
            WorkEndedAt = r.WorkEndedAt,
            CategoryId = r.CategoryId,
            CategoryName = r.Category?.Name ?? "General",
            SlaHours = r.Category?.SlaHours ?? 0,
            ResolutionNotes = r.ResolutionNotes
        };

        
        if (r.WorkStartedAt.HasValue && r.WorkEndedAt.HasValue)
        {
            var diff = r.WorkEndedAt.Value - r.WorkStartedAt.Value;
            dto.Duration = $"{(int)diff.TotalMinutes} mins";
        }

        return dto;

    }
}