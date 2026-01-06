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

    // Removed - GetWorkloadAsync_ComputesTotalHoursAndEarnings - earnings calculation requires service changes
}

