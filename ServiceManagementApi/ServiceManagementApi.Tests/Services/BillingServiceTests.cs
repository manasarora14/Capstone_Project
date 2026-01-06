using Microsoft.EntityFrameworkCore;
using ServiceManagementApi.Data;
using ServiceManagementApi.Models;
using ServiceManagementApi.Services;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ServiceManagementApi.Tests.Services;

public class BillingServiceTests
{
    private ApplicationDbContext CreateContext(string dbName)
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(opts);
    }

    [Fact]
    public async Task PayInvoiceAsync_MarksPaidAndClosesRequest()
    {
        var ctx = CreateContext(nameof(PayInvoiceAsync_MarksPaidAndClosesRequest));
        var req = new ServiceRequest
        {
            Id = 1,
            Status = RequestStatus.Completed,
            IssueDescription = "Test issue", // Added required property
            CustomerId = "cust1"             // Added required property
        };
        var invoice = new Invoice { Id = 1, ServiceRequestId = 1, Amount = 100, Status = "Pending" };
        ctx.ServiceRequests.Add(req);
        ctx.Invoices.Add(invoice);
        await ctx.SaveChangesAsync();

        var svc = new BillingService(ctx);

        var ok = await svc.PayInvoiceAsync(1, "user1");

        Assert.True(ok);
        var updated = await ctx.Invoices.FindAsync(1);
        Assert.Equal("Paid", updated!.Status);
        var updatedReq = await ctx.ServiceRequests.FindAsync(1);
        Assert.Equal(RequestStatus.Closed, updatedReq!.Status);
    }

    [Fact]
    public async Task GetInvoicesAsync_FiltersByCustomer()
    {
        var ctx = CreateContext(nameof(GetInvoicesAsync_FiltersByCustomer));
        ctx.ServiceRequests.Add(new ServiceRequest { Id = 1, CustomerId = "cust1", IssueDescription = "Test issue" });
        ctx.Invoices.Add(new Invoice { Id = 1, ServiceRequestId = 1, Amount = 50, Status = "Pending" });
        ctx.ServiceRequests.Add(new ServiceRequest { Id = 2, CustomerId = "cust2", IssueDescription = "Test issue 2" });
        ctx.Invoices.Add(new Invoice { Id = 2, ServiceRequestId = 2, Amount = 70, Status = "Pending" });
        await ctx.SaveChangesAsync();

        var svc = new BillingService(ctx);

        var mine = await svc.GetInvoicesAsync("cust1", "Customer");

        Assert.Single(mine);
        Assert.Equal(1, mine.First().Id);
    }

    [Fact]
    public async Task CreateInvoiceAsync_AddsInvoiceIfMissing()
    {
        var ctx = CreateContext(nameof(CreateInvoiceAsync_AddsInvoiceIfMissing));
        _ = ctx.ServiceRequests.Add(new ServiceRequest
        {
            Id = 1,
            Category = new ServiceCategory { BaseCharge = 100, SlaHours = 4 },
            Priority = Priority.Medium,
            IssueDescription = "Test issue", // Added required property
            CustomerId = "cust1"             // Added required property
        });
        await ctx.SaveChangesAsync();
        var svc = new BillingService(ctx);

        await svc.CreateInvoiceAsync(1);

        Assert.Equal(1, ctx.Invoices.Count());
        var inv = await ctx.Invoices.FirstAsync();
        Assert.True(inv.Amount > 0);
    }
}

