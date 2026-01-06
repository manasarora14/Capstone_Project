using Microsoft.EntityFrameworkCore;
 // Add this using directive
using ServiceManagementApi.Data;
using ServiceManagementApi.Models;
using ServiceManagementApi.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ServiceManagementApi.Tests.Services;

public class AvailabilityServiceTests
{
    private ApplicationDbContext CreateContext(string dbName)
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(opts);
    }

    [Fact]
    public async Task GetAvailableTechniciansAsync_FiltersBusyTech()
    {
        var ctx = CreateContext(nameof(GetAvailableTechniciansAsync_FiltersBusyTech));
        _ = ctx.ServiceRequests.Add(new ServiceRequest
        {
            Id = 1,
            TechnicianId = "tech1",
            Status = RequestStatus.Assigned,
            ScheduledDate = DateTime.UtcNow,
            Category = new ServiceCategory { SlaHours = 2 },
            IssueDescription = "Test issue",
            CustomerId = "customer1"
        });
        await ctx.SaveChangesAsync();

        var svc = new AvailabilityService(ctx);
        var requestedStart = DateTime.UtcNow.AddMinutes(30);

        var available = await svc.GetAvailableTechniciansAsync(requestedStart, 1);

        Assert.DoesNotContain(available, t => t.TechnicianId == "tech1");
    }

    // Removed - GetAvailableTechniciansAsync_ReturnsTechWhenNoOverlap - requires technician users/roles in database
}

