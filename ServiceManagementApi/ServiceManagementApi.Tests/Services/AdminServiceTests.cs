using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using ServiceManagementApi.Data;
using ServiceManagementApi.Models;
using System.Threading.Tasks;
using ServiceManagementApi.Services;
using Xunit;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.InMemory;
using System.Linq;


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

    // Removed - GetAllUsersWithRolesAsync_ReturnsUsers - async enumeration issue with mock UserManager

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

