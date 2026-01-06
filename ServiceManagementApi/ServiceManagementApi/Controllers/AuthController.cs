using Microsoft.AspNetCore.Mvc;
using ServiceManagementApi.DTOs;
using ServiceManagementApi.Services;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        if (!result.Succeeded) return BadRequest(result.Errors);
        return Ok(new { message = "Registration Successful" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var response = await _authService.LoginAsync(dto);
        if (response == null) return Unauthorized("Invalid Credentials");
        return Ok(response);
    }
}