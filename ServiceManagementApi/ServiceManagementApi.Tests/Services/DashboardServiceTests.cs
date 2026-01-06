using Microsoft.EntityFrameworkCore;
using ServiceManagementApi.Data;
using ServiceManagementApi.Models;
using System.Threading.Tasks;
using ServiceManagementApi.Services;
using Xunit;
using System.Collections.Generic;
using System;
using System.Linq;

namespace ServiceManagementApi.Tests.Services;

public class DashboardServiceTests
{
    private ApplicationDbContext CreateContext(string dbName)
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(opts);
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsStatusCountsAndRevenue()
    {
        var ctx = CreateContext(nameof(GetStatsAsync_ReturnsStatusCountsAndRevenue));
        ctx.ServiceRequests.AddRange(
            new ServiceRequest { Id = 1, Status = RequestStatus.Requested, IssueDescription = "Test Issue 1", CustomerId = "c1" },
            new ServiceRequest { Id = 2, Status = RequestStatus.Assigned, TechnicianId = "t1", IssueDescription = "Test Issue 2", CustomerId = "c2" }
        );
        ctx.Invoices.Add(new Invoice { Id = 1, ServiceRequestId = 2, Amount = 200, Status = "Paid", PaidAt = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        var svc = new DashboardService(ctx);

        var stats = await svc.GetStatsAsync() as dynamic;

        Assert.Equal(2, (int)stats.TotalRequests);
        Assert.True(((IEnumerable<object>)stats.StatusSummary).Any());
        Assert.Equal(200, (decimal)stats.TotalRevenue);
    }
}

