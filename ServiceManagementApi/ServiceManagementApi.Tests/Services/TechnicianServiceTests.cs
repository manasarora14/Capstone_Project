using Microsoft.EntityFrameworkCore;
using ServiceManagementApi.Data;
using ServiceManagementApi.Models;
using ServiceManagementApi.Services;
using System;
using System.Threading.Tasks;   
using System.Linq;
using Xunit;

namespace ServiceManagementApi.Tests.Services;

public class TechnicianServiceTests
{
    private ApplicationDbContext CreateContext(string dbName)
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(opts);
    }

    [Fact]
    public async Task GetWorkloadAsync_ComputesTotalHoursAndEarnings()
    {
        var ctx = CreateContext(nameof(GetWorkloadAsync_ComputesTotalHoursAndEarnings));
        ctx.ServiceRequests.Add(new ServiceRequest
        {
            Id = 1,
            TechnicianId = "tech1",
            Status = RequestStatus.Completed,
            WorkStartedAt = DateTime.UtcNow.AddHours(-2),
            WorkEndedAt = DateTime.UtcNow.AddHours(-1),
            TotalPrice = 150,
            Category = new ServiceCategory { BaseCharge = 100, SlaHours = 4 },
            IssueDescription = "Test issue", // Fix for CS9035: required property
            CustomerId = "customer1"         // Fix for CS9035: required property
        });
        ctx.Invoices.Add(new Invoice { Id = 1, ServiceRequestId = 1, Amount = 150, Status = "Paid" });
        await ctx.SaveChangesAsync();

        var svc = new TechnicianService(ctx);

        var workload = await svc.GetWorkloadAsync("tech1");

        Assert.NotNull(workload);
        Assert.True(workload.TotalHoursWorked > 0);
        Assert.True(workload.TotalEarnings >= 150);
        Assert.Single(workload.PreviousTasks!);
    }
}

