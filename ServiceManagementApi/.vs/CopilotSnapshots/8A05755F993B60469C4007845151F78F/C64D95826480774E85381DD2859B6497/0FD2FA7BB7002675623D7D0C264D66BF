using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceManagementApi.Services;
using ServiceManagementApi.DTOs;

namespace ServiceManagementApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Manager")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _adminService.GetAllUsersWithRolesAsync();
        return Ok(users);
    }

    [HttpPost("update-role")]
    public async Task<IActionResult> UpdateRole([FromBody] UpdateRoleDto model)
    {
        var result = await _adminService.UpdateUserRoleAsync(model.UserId, model.NewRole);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { message = $"Role updated to {model.NewRole} successfully" });
    }
}