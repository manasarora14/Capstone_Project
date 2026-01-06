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

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _controller = new AuthController(_mockAuthService.Object);
    }

    // TC-01: FR-AUTH-01 - User Registration with Valid Data
    [Fact]
    public async Task Register_WithValidData_ReturnsOk()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test@123456",
            Role = "Customer"
        };

        var identityResult = IdentityResult.Success;
        _mockAuthService.Setup(s => s.RegisterAsync(registerDto))
            .ReturnsAsync(identityResult);

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockAuthService.Verify(s => s.RegisterAsync(registerDto), Times.Once);
    }

    // TC-02: FR-AUTH-01 - User Registration with Invalid Email
    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "invalid-email",
            Password = "Test@123456",
            Role = "Customer"
        };

        var identityResult = IdentityResult.Failed(new IdentityError 
        { 
            Code = "InvalidEmail", 
            Description = "Invalid email format" 
        });
        
        _mockAuthService.Setup(s => s.RegisterAsync(registerDto))
            .ReturnsAsync(identityResult);

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    // TC-03: FR-AUTH-02 - Login with Valid Credentials
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Test@123456"
        };

        var authResponse = new AuthResponseDto
        {
            Token = "valid-jwt-token",
            Email = "test@example.com",
            Role = "Customer"
        };

        _mockAuthService.Setup(s => s.LoginAsync(loginDto))
            .ReturnsAsync(authResponse);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponseDto>(okResult.Value);
        Assert.Equal("valid-jwt-token", response.Token);
        Assert.Equal("test@example.com", response.Email);
        Assert.Equal("Customer", response.Role);
    }

    // TC-04: FR-AUTH-02 - Login with Invalid Credentials
    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        _mockAuthService.Setup(s => s.LoginAsync(loginDto))
            .ReturnsAsync((AuthResponseDto?)null);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid Credentials", unauthorizedResult.Value);
    }

    // TC-05: FR-AUTH-03 - JWT Token Generation on Successful Login
    [Fact]
    public async Task Login_ValidCredentials_GeneratesJWTToken()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "tech@example.com",
            Password = "Test@123456"
        };

        var authResponse = new AuthResponseDto
        {
            Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
            Email = "tech@example.com",
            Role = "Technician"
        };

        _mockAuthService.Setup(s => s.LoginAsync(loginDto))
            .ReturnsAsync(authResponse);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponseDto>(okResult.Value);
        Assert.NotEmpty(response.Token);
        Assert.StartsWith("eyJ", response.Token); // JWT tokens start with "eyJ"
    }
}

