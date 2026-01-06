using Microsoft.EntityFrameworkCore;
using ServiceManagementApi.Data;
using ServiceManagementApi.Models;
using ServiceManagementApi.Services;
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
        ctx.ServiceRequests.Add(new ServiceRequest
        {
            Id = 1,
            TechnicianId = "tech1",
            Status = RequestStatus.Assigned,
            ScheduledDate = DateTime.UtcNow,
            Category = new ServiceCategory { SlaHours = 2 }
        });
        await ctx.SaveChangesAsync();

        var svc = new AvailabilityService(ctx);
        var requestedStart = DateTime.UtcNow.AddMinutes(30);

        var available = await svc.GetAvailableTechniciansAsync(requestedStart, 1);

        Assert.DoesNotContain(available, t => t.TechnicianId == "tech1");
    }

    [Fact]
    public async Task GetAvailableTechniciansAsync_ReturnsTechWhenNoOverlap()
    {
        var ctx = CreateContext(nameof(GetAvailableTechniciansAsync_ReturnsTechWhenNoOverlap));
        ctx.ServiceRequests.Add(new ServiceRequest
        {
            Id = 1,
            TechnicianId = "tech1",
            Status = RequestStatus.Assigned,
            ScheduledDate = DateTime.UtcNow,
            Category = new ServiceCategory { SlaHours = 1 }
        });
        await ctx.SaveChangesAsync();

        var svc = new AvailabilityService(ctx);
        var requestedStart = DateTime.UtcNow.AddHours(5);

        var available = await svc.GetAvailableTechniciansAsync(requestedStart, 1);

        // tech1 should be considered available since windows don't overlap
        Assert.Contains(available, t => t.TechnicianId == "tech1");
    }
}

