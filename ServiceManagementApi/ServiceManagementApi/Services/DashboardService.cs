using Microsoft.EntityFrameworkCore;
using ServiceManagementApi.Data;
using System.Linq;

namespace ServiceManagementApi.Services;

public interface IDashboardService
{
    
    Task<object> GetStatsAsync();

    
    Task<object> GetDashboardStatsAsync();
}

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    public DashboardService(ApplicationDbContext context) => _context = context;

    
    public async Task<object> GetStatsAsync()
    {
       
        var statusCounts = await _context.ServiceRequests
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        
        var techWorkload = await _context.ServiceRequests
            .Where(r => r.TechnicianId != null)
            .Include(r => r.Technician) 
            .GroupBy(r => new { r.TechnicianId, r.Technician.UserName, r.Technician.Email })
            .Select(g => new {
                Technician = g.Key.UserName ?? g.Key.Email,
                TaskCount = g.Count()
            })
            .ToListAsync();

  
        var categoryCounts = await _context.ServiceRequests
            .Include(r => r.Category)
            .GroupBy(r => r.Category.Name)
            .Select(g => new {
                Category = g.Key ?? "Uncategorized",
                Count = g.Count()
            })
            .ToListAsync();

        
        var completedRequests = await _context.ServiceRequests
            .Where(r => r.WorkStartedAt != null && r.WorkEndedAt != null)
            .ToListAsync();

        double avgRes = 0;
        if (completedRequests.Any())
        {
            avgRes = completedRequests.Average(r =>
                (r.WorkEndedAt.Value - r.WorkStartedAt.Value).TotalHours);
        }

        
        var revenueReport = await _context.Invoices
        .Where(i => i.Status == "Paid" && i.PaidAt != null)
        .GroupBy(i => new { i.PaidAt.Value.Month, i.PaidAt.Value.Year })
        .Select(g => new {
            Month = $"{g.Key.Month}/{g.Key.Year}",
            Total = g.Sum(i => i.Amount) 
        })
        .ToListAsync();

        return new
        {
            TotalRequests = await _context.ServiceRequests.CountAsync(),
            StatusSummary = statusCounts,
            Workload = techWorkload,
            CategoryCounts = categoryCounts,
            AvgResolutionTime = Math.Max(0, avgRes),
            RevenueReport = revenueReport,
            TotalRevenue = await _context.Invoices.Where(i => i.Status == "Paid").SumAsync(i => i.Amount)
        };
    }

    
    public async Task<object> GetDashboardStatsAsync()
    {
        return await GetStatsAsync();
    }
}