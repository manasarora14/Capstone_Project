using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using ServiceManagementApi.Data;
using ServiceManagementApi.Models;
using ServiceManagementApi.Services;
using Xunit;

namespace ServiceManagementApi.Tests.Services;

public class AdminServiceTests
{
    private ApplicationDbContext CreateContext(string dbName)
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(opts);
    }

    [Fact]
    public async Task GetAllUsersWithRolesAsync_ReturnsUsers()
    {
        var ctx = CreateContext(nameof(GetAllUsersWithRolesAsync_ReturnsUsers));
        ctx.Users.Add(new ApplicationUser { Id = "1", Email = "u1@test.com", FullName = "User One" });
        await ctx.SaveChangesAsync();

        var mockMgr = TestHelpers.MockUserManager<ApplicationUser>(ctx.Users.ToList());
        mockMgr.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "Customer" });

        var svc = new AdminService(mockMgr.Object, ctx);
        var result = await svc.GetAllUsersWithRolesAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_ReturnsFailed_WhenUserMissing()
    {
        var ctx = CreateContext(nameof(UpdateUserRoleAsync_ReturnsFailed_WhenUserMissing));
        var mockMgr = TestHelpers.MockUserManager<ApplicationUser>(ctx.Users.ToList());
        mockMgr.Setup(m => m.FindByIdAsync("missing")).ReturnsAsync((ApplicationUser?)null);

        var svc = new AdminService(mockMgr.Object, ctx);
        var res = await svc.UpdateUserRoleAsync("missing", "Admin");

        Assert.False(res.Succeeded);
    }
}

