using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServiceManagementApi.Data;
using ServiceManagementApi.DTOs;
using ServiceManagementApi.Models;

namespace ServiceManagementApi.Services;

public interface IAvailabilityService
{
    Task<List<AvailableTechDto>> GetAvailableTechniciansAsync(DateTime scheduledUtc, double durationHours);
}

public class AvailabilityService : IAvailabilityService
{
    private readonly ApplicationDbContext _context;

    public AvailabilityService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AvailableTechDto>> GetAvailableTechniciansAsync(DateTime scheduledUtc, double durationHours)
    {
        var requestedStart = scheduledUtc;
        var requestedEnd = scheduledUtc.AddHours(durationHours);

        
        var requests = await _context.ServiceRequests
            .Include(r => r.Category)
            .Where(r => r.TechnicianId != null && (r.Status == RequestStatus.Assigned || r.Status == RequestStatus.InProgress))
            .ToListAsync();

        var busyTechs = new HashSet<string>();

        foreach (var r in requests)
        {
            
            var busyStart = r.PlannedStartUtc ?? r.ScheduledDate;

           
            var dur = r.EstimatedDurationHours ?? r.Category?.SlaHours ?? 0;
            if (dur <= 0)
            {
                
                continue;
            }

            var busyEnd = busyStart.AddHours(dur);

            
            if (busyStart <= requestedEnd && requestedStart <= busyEnd)
            {
                if (!string.IsNullOrEmpty(r.TechnicianId)) busyTechs.Add(r.TechnicianId!);
            }
        }

        
        var techRoleId = await _context.Roles.Where(r => r.Name == "Technician").Select(r => r.Id).FirstOrDefaultAsync();
        if (techRoleId == null) return new List<AvailableTechDto>();

        var techUserIds = await _context.Set<IdentityUserRole<string>>()
            .Where(ur => ur.RoleId == techRoleId)
            .Select(ur => ur.UserId)
            .ToListAsync();

        var availableIds = techUserIds.Except(busyTechs).ToList();

        if (!availableIds.Any()) return new List<AvailableTechDto>();

        var users = await _context.Users.Where(u => availableIds.Contains(u.Id)).Select(u => new { u.Id, u.UserName }).ToListAsync();

        var result = users.Select(u => new AvailableTechDto { TechnicianId = u.Id, UserName = u.UserName, NextAvailableFromUtc = null }).ToList();

        return result;
    }
}
