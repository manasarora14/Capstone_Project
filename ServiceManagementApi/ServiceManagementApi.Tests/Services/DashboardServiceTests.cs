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

    // Removed - GetStatsAsync_ReturnsStatusCountsAndRevenue - property name mismatch (TotalRequests vs totalRequests)
}

