using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceManagementApi.DTOs;
using ServiceManagementApi.Services;
using System.Security.Claims;

namespace ServiceManagementApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TechnicianController : ControllerBase
{
    private readonly ITechnicianService _service;

    public TechnicianController(ITechnicianService service) => _service = service;

    
    [HttpPost("availability")]
    [Authorize(Roles = "Technician")]
    public async Task<IActionResult> AddAvailability([FromBody] AvailabilityDto dto)
    {
       
        var techId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (techId != dto.TechnicianId) return Forbid();

        var created = await _service.AddAvailabilityAsync(dto);
        return CreatedAtAction(nameof(GetAvailability), new { technicianId = created.TechnicianId }, created);
    }

    [HttpGet("availability/{technicianId}")]
    [Authorize(Roles = "Technician,Manager,Admin")]
    public async Task<IActionResult> GetAvailability(string technicianId)
    {
        var list = await _service.GetAvailabilityAsync(technicianId);
        return Ok(list);
    }

    [HttpDelete("availability/{id}")]
    [Authorize(Roles = "Technician")]
    public async Task<IActionResult> DeleteAvailability(int id)
    {
        var techId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var success = await _service.RemoveAvailabilityAsync(id, techId);
        return success ? Ok() : NotFound();
    }

    
    [HttpPost("schedule")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> ScheduleVisit([FromBody] ScheduleVisitDto dto)
    {
        var success = await _service.ScheduleVisitAsync(dto);
        return success ? Ok() : BadRequest("Technician not available or conflict present");
    }

    
    [HttpGet("workload")]
    [Authorize(Roles = "Technician")]
    public async Task<IActionResult> GetWorkload()
    {
        var techId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(techId)) return Unauthorized();

        var report = await _service.GetWorkloadAsync(techId);
        return Ok(report);
    }
}
