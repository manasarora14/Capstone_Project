using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using ServiceManagementApi.DTOs;
using ServiceManagementApi.Models;
using ServiceManagementApi.Services;
using Xunit;

namespace ServiceManagementApi.Tests.Services;

public class AuthServiceTests
{
    private IConfiguration MockConfig()
    {
        var dict = new Dictionary<string, string?>
        {
            { "Jwt:Key", "VerySecretKeyForTests12345" },
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" }
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict!).Build();
    }

    [Fact]
    public async Task RegisterAsync_CreatesUserAndAddsRole()
    {
        var mockMgr = TestHelpers.MockUserManager<ApplicationUser>();
        mockMgr.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "Pass@123"))
            .ReturnsAsync(IdentityResult.Success);
        mockMgr.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Customer"))
            .ReturnsAsync(IdentityResult.Success);

        var svc = new AuthService(mockMgr.Object, MockConfig());

        var result = await svc.RegisterAsync(new RegisterDto { Email = "u@test.com", Password = "Pass@123", Role = "Customer" });

        Assert.True(result.Succeeded);
        mockMgr.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Customer"), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ReturnsToken_WhenValidCredentials()
    {
        var user = new ApplicationUser { Email = "u@test.com", Id = "u1" };
        var roles = new List<string> { "Customer" };
        var mockMgr = TestHelpers.MockUserManager<ApplicationUser>(new List<ApplicationUser> { user });

        mockMgr.Setup(m => m.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        mockMgr.Setup(m => m.CheckPasswordAsync(user, "Pass@123")).ReturnsAsync(true);
        mockMgr.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(roles);

        var svc = new AuthService(mockMgr.Object, MockConfig());

        var resp = await svc.LoginAsync(new LoginDto { Email = user.Email!, Password = "Pass@123" });

        Assert.NotNull(resp);
        Assert.False(string.IsNullOrWhiteSpace(resp!.Token));
        Assert.Equal("Customer", resp.Role);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_WhenInvalidPassword()
    {
        var user = new ApplicationUser { Email = "u@test.com", Id = "u1" };
        var mockMgr = TestHelpers.MockUserManager<ApplicationUser>(new List<ApplicationUser> { user });
        mockMgr.Setup(m => m.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        mockMgr.Setup(m => m.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);

        var svc = new AuthService(mockMgr.Object, MockConfig());
        var resp = await svc.LoginAsync(new LoginDto { Email = user.Email!, Password = "wrong" });

        Assert.Null(resp);
    }
}

