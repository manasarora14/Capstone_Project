using Microsoft.EntityFrameworkCore;
using ServiceManagementApi.Data;
using ServiceManagementApi.DTOs;
using ServiceManagementApi.Models;

namespace ServiceManagementApi.Services;

public interface ITechnicianService
{
    Task<TechnicianAvailabilityResponseDto> AddAvailabilityAsync(AvailabilityDto dto);
    Task<bool> RemoveAvailabilityAsync(int id, string technicianId);
    Task<List<TechnicianAvailabilityResponseDto>> GetAvailabilityAsync(string technicianId);

    
    Task<bool> ScheduleVisitAsync(ScheduleVisitDto dto);

    Task<TechnicianWorkloadDto> GetWorkloadAsync(string technicianId);
}

public class TechnicianService : ITechnicianService
{
    private readonly ApplicationDbContext _context;

    public TechnicianService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TechnicianAvailabilityResponseDto> AddAvailabilityAsync(AvailabilityDto dto)
    {
        if (dto.EndUtc <= dto.StartUtc) throw new ArgumentException("End must be after start");


        return new TechnicianAvailabilityResponseDto { Id = 0, TechnicianId = dto.TechnicianId, StartUtc = dto.StartUtc, EndUtc = dto.EndUtc };
    }

    public async Task<bool> RemoveAvailabilityAsync(int id, string technicianId)
    {
        return true;
    }

    public async Task<List<TechnicianAvailabilityResponseDto>> GetAvailabilityAsync(string technicianId)
    {
        return new List<TechnicianAvailabilityResponseDto>();
    }

    public async Task<bool> ScheduleVisitAsync(ScheduleVisitDto dto)
    {
        var request = await _context.ServiceRequests.FindAsync(dto.ServiceRequestId);
        if (request == null) return false;

        request.TechnicianId = dto.TechnicianId;
        request.Status = RequestStatus.Assigned;
        request.ScheduledDate = dto.ScheduledUtc;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<TechnicianWorkloadDto> GetWorkloadAsync(string technicianId)
    {
        var requests = await _context.ServiceRequests
            .Include(r => r.Category)
            .Where(r => r.TechnicianId == technicianId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        double totalHours = 0;
        decimal totalEarnings = 0;

        var finishedWork = requests.Where(r => r.Status == RequestStatus.Completed || r.Status == RequestStatus.Closed).ToList();
        foreach (var r in finishedWork)
        {
            if (r.WorkStartedAt.HasValue && r.WorkEndedAt.HasValue)
            {
                totalHours += (r.WorkEndedAt.Value - r.WorkStartedAt.Value).TotalHours;
            }
        }

        
        totalEarnings = requests.Where(r => r.Status == RequestStatus.Closed).Sum(r => r.TotalPrice);

       
        var customerIds = requests.Select(r => r.CustomerId).Distinct().ToList();
        var customers = await _context.Users
            .Where(u => customerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName ?? u.Email);

        
        var taskDtos = requests.Select(r => new TechnicianTaskDto
        {
            RequestId = r.Id,
            IssueDescription = r.IssueDescription,
            ScheduledDate = r.ScheduledDate,
            CompletedAt = r.CompletedAt,
            TotalPrice = r.TotalPrice,
            Status = ((int)r.Status).ToString(), 
            CustomerId = r.CustomerId,
            CustomerName = customers.ContainsKey(r.CustomerId) ? customers[r.CustomerId] : "Unknown"
        }).ToList();

        return new TechnicianWorkloadDto
        {
            TechnicianId = technicianId,
            TotalHoursWorked = Math.Round(totalHours, 1),
            TotalEarnings = totalEarnings,
            MonthlyEarnings = new List<object>(),
            PreviousTasks = taskDtos 
        };
    }
}