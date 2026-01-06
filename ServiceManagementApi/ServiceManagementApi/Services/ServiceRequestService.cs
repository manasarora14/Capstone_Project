using Microsoft.EntityFrameworkCore;
using ServiceManagementApi.Data;
using ServiceManagementApi.DTOs;
using ServiceManagementApi.Models;
using static Azure.Core.HttpHeader;

namespace ServiceManagementApi.Services;

public interface IServiceRequestService
{
    Task<List<CategoryResponseDto>> GetCategoriesAsync();
    Task CreateRequestAsync(CreateRequestDto dto, string userId);
    Task<List<ServiceRequest>> GetCustomerRequestsAsync(string userId);
    Task<List<ServiceRequest>> GetTechnicianTasksAsync(string techId);
    Task<List<ServiceRequest>> GetAllForMonitorAsync();
    Task<ServiceRequest?> GetServiceRequestByIdAsync(int id, string userId, string userRole);
    Task<bool> AssignTechnicianAsync(AssignTechnicianDto dto);
    Task<bool> UpdateStatusWithBillingAsync(UpdateStatusDto dto);

    Task<object> GetDashboardStatsAsync();

    
    Task<bool> RespondToAssignmentAsync(int requestId, string techId, bool accept, DateTime? plannedStartUtc = null);
    Task<bool> RescheduleRequestAsync(int requestId, DateTime newDate, string userId, string userRole);
    Task<bool> CancelRequestAsync(int requestId, string userId);

    
    Task<bool> StartWorkAsync(int requestId, string techId, DateTime startUtc);
    Task<bool> FinishWorkAsync(int requestId, string techId, DateTime endUtc, string? notes = null);

    Task<PagedResponse<ServiceRequest>> GetCustomerRequestsAsync(string userId, QueryParameters query);
    Task<PagedResponse<ServiceRequest>> GetTechnicianTasksAsync(string techId, QueryParameters query);
    Task<PagedResponse<ServiceRequest>> GetAllForMonitorAsync(QueryParameters query);
}

public partial class ServiceRequestService : IServiceRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly IBillingService _billingService;
    private readonly INotificationQueue _notificationQueue; 

    public ServiceRequestService(ApplicationDbContext context, IBillingService billingService, INotificationQueue notificationQueue) // Update the constructor to accept INotificationQueue
    {
        _context = context;
        _billingService = billingService;
        _notificationQueue = notificationQueue;
    }

    
    public async Task<List<CategoryResponseDto>> GetCategoriesAsync() =>
        await _context.ServiceCategories
            .Select(sc => new CategoryResponseDto {
                Id = sc.Id,
                Name = sc.Name,
                Description = sc.Description,
                BaseCharge = sc.BaseCharge,
                SlaHours = sc.SlaHours,
                DisplaySla = sc.Name == "Installation" ? "24 hours" : sc.Name == "Maintenance" ? "36 hours" : sc.Name == "Repair" ? "12 hours" : (sc.SlaHours + " hours")
            })
            .ToListAsync();

    public async Task CreateRequestAsync(CreateRequestDto dto, string userId)
    {
        var category = await _context.ServiceCategories.FindAsync(dto.CategoryId);
        var categoryName = category?.Name ?? "Service";

        var request = new ServiceRequest
        {
            CustomerId = userId,
            IssueDescription = dto.IssueDescription,
            CategoryId = dto.CategoryId,
            Priority = dto.Priority,
            Status = RequestStatus.Requested,
            ScheduledDate = (dto.ScheduledDate.Date + (dto.ScheduledTime ?? TimeSpan.Zero)).ToUniversalTime()
        };

        _context.ServiceRequests.Add(request);
        await _context.SaveChangesAsync();

        
        _notificationQueue.Enqueue(request.CustomerId,
            $"Success! Your request for {categoryName} has been submitted (ID: #{request.Id}).");
    }

    public async Task<List<ServiceRequest>> GetCustomerRequestsAsync(string userId)
    {
        return await _context.ServiceRequests
            .Include(r => r.Category)
            .Include(r => r.Technician)
            .Where(r => r.CustomerId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }




    public async Task<List<ServiceRequest>> GetTechnicianTasksAsync(string techId) =>
        await _context.ServiceRequests
            .Include(r => r.Category).Include(r => r.Customer)
            .Where(r => r.TechnicianId == techId)
            .OrderByDescending(r => r.CreatedAt).ToListAsync();

    public async Task<List<ServiceRequest>> GetAllForMonitorAsync() =>
        await _context.ServiceRequests
            .Include(r => r.Category).Include(r => r.Customer).Include(r => r.Technician)
            .OrderByDescending(r => r.CreatedAt).ToListAsync();

    public async Task<ServiceRequest?> GetServiceRequestByIdAsync(int id, string userId, string userRole)
    {
        var request = await _context.ServiceRequests
            .Include(r => r.Category)
            .Include(r => r.Customer)
            .Include(r => r.Technician)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null) return null;

        if (userRole == "Manager" || userRole == "Admin")
        {
            return request;
        }

        if (userRole == "Customer")
        {
            if (request.CustomerId != userId)
            {
                return null; 
            }
            return request;
        }

       
        if (userRole == "Technician")
        {
            if (request.TechnicianId != userId)
            {
                return null; 
            }
            return request;
        }

        return null;
    }

    public async Task<bool> AssignTechnicianAsync(AssignTechnicianDto dto)
    {
        var request = await _context.ServiceRequests
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == dto.RequestId);

        if (request == null) return false;

       

        request.TechnicianId = dto.TechnicianId;
        request.Status = RequestStatus.Assigned;
        await _context.SaveChangesAsync();

        
        _notificationQueue.Enqueue(request.TechnicianId,
            $"New Task: You have been assigned to Service Request #{request.Id}.");

        _notificationQueue.Enqueue(request.CustomerId,
            $"Update: A technician has been assigned to your request #{request.Id}.");

        return true;
    }

    public async Task<bool> UpdateStatusWithBillingAsync(UpdateStatusDto dto)
    {
        var request = await _context.ServiceRequests
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == dto.RequestId);

        if (request == null) return false;

        switch (dto.Status)
        {
            case RequestStatus.Completed:
                var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);

                request.WorkEndedAt = nowIst;
                request.CompletedAt = nowIst;
                request.Status = RequestStatus.Completed;

               
                request.TotalPrice = request.Category?.BaseCharge ?? 0m;

                await _billingService.CreateInvoiceAsync(request.Id);
                break;

            case RequestStatus.Cancelled:
                request.Status = RequestStatus.Cancelled;
                request.TotalPrice = 0;
                break;

            default:
                request.Status = dto.Status;
                break;
        }

        request.ResolutionNotes = dto.ResolutionNotes;
        await _context.SaveChangesAsync();
        return true;
    }
    public async Task<bool> StartWorkAsync(int id, string techId, DateTime startTime)
    {
        var request = await _context.ServiceRequests.FindAsync(id);
        if (request == null || request.TechnicianId != techId) return false;

        var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        request.WorkStartedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);
        request.Status = RequestStatus.InProgress;

        await _context.SaveChangesAsync();

        _notificationQueue.Enqueue(request.CustomerId,
            $"Technician has started work on your request #{request.Id}.");

        return true;
    }

    public async Task<bool> FinishWorkAsync(int requestId, string techId, DateTime endUtc, string? notes = null)
    {
        var request = await _context.ServiceRequests
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null || request.TechnicianId != techId) return false;

        var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        var currentTimeIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);

        request.WorkEndedAt = currentTimeIst;
        request.CompletedAt = currentTimeIst; // Keep these consistent in the same zone
        request.Status = RequestStatus.Completed;


        if (request.TotalPrice == 0m)
        {
            request.TotalPrice = request.Category?.BaseCharge ?? 0m;
        }
        request.ResolutionNotes = notes;
        await _context.SaveChangesAsync();
        await _billingService.CreateInvoiceAsync(request.Id);

        _notificationQueue.Enqueue(request.CustomerId,
            $"Work completed on Request #{request.Id}. Your invoice has been generated.");

        return true;
    }


    public async Task<object> GetDashboardStatsAsync()
    {
        
        var allRequests = await _context.ServiceRequests.Include(r => r.Category).Include(r => r.Technician).ToListAsync();
        var invoices = await _context.Invoices.ToListAsync();
        var now = DateTime.UtcNow;

      
        var totalRequests = allRequests.Count;
        var statusSummary = allRequests
            .GroupBy(r => r.Status)
            .Select(g => new { status = g.Key.ToString(), count = g.Count() })
            .ToList();

        var categoryCounts = allRequests.Where(r => r.Category != null)
         .GroupBy(r => r.Category!.Name)
         .Select(g => new { category = g.Key, count = g.Count() });


        var workload = allRequests
    .Where(r => r.TechnicianId != null && r.Status != RequestStatus.Closed && r.Status != RequestStatus.Cancelled)
    .GroupBy(r => !string.IsNullOrEmpty(r.Technician?.FullName) ? r.Technician.FullName : (r.Technician?.UserName ?? "Unassigned"))
    .Select(g => new { technician = g.Key, taskCount = g.Count() });




        double avgResolutionTime = 0;
        var completed = allRequests.Where(r => r.WorkStartedAt.HasValue && r.WorkEndedAt.HasValue).ToList();
        if (completed.Any())
        {
            avgResolutionTime = Math.Round(completed.Average(r => (r.WorkEndedAt!.Value - r.WorkStartedAt!.Value).TotalHours), 1);
        }

        double slaCompliance = 100;
        var slaTasks = completed.Where(r => r.Category != null).ToList();
        if (slaTasks.Any())
        {
            var compliant = slaTasks.Count(r => (r.WorkEndedAt!.Value - r.WorkStartedAt!.Value).TotalHours <= r.Category!.SlaHours);
            slaCompliance = Math.Round(100.0 * compliant / slaTasks.Count, 1);
        }

        
        decimal totalRevenue = invoices
            .Where(i => i.PaidAt.HasValue && i.PaidAt.Value.Month == now.Month && i.PaidAt.Value.Year == now.Year)
            .Sum(i => (decimal?)i.Amount) ?? 0m;

       
        if (totalRevenue == 0m)
        {
            totalRevenue = allRequests
                .Where(r => r.Status == RequestStatus.Closed && r.CompletedAt.HasValue
                       && r.CompletedAt.Value.Month == now.Month && r.CompletedAt.Value.Year == now.Year)
                .Sum(r => r.TotalPrice);
        }

      
       

        
        var revenueReport = invoices
            .Where(i => i.PaidAt.HasValue)
            .GroupBy(i => new { i.PaidAt!.Value.Year, i.PaidAt.Value.Month })
            .Select(g => new {
                month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                total = g.Sum(i => i.Amount)
            })
            .ToList();

        
        if (!revenueReport.Any())
        {
            revenueReport = allRequests
                .Where(r => r.Status == RequestStatus.Closed && r.CompletedAt.HasValue)
                .GroupBy(r => new { r.CompletedAt!.Value.Year, r.CompletedAt.Value.Month })
                .Select(g => new {
                    month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    total = g.Sum(r => r.TotalPrice)
                })
                .OrderByDescending(r => r.month)
                .ToList();
        }

        return new
        {
            totalRequests = allRequests.Count,
            statusSummary = allRequests.GroupBy(r => r.Status).Select(g => new { status = g.Key.ToString(), count = g.Count() }),
            workload,
            categoryCounts,
            avgResolutionTime,
            slaCompliance,
            revenueReport,
            totalRevenue
         
        };
    }

    public async Task<bool> UpdateStatusAsync(int requestId, RequestStatus newStatus, string notes)
    {
        var request = await _context.ServiceRequests
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null) return false;

        if (request.Status == RequestStatus.Closed || request.Status == RequestStatus.Cancelled)
        {
            return false;
        }

        request.Status = newStatus;
        request.ResolutionNotes = notes;

        if (newStatus == RequestStatus.Completed)
        {
            var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var currentTimeIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);

            request.WorkEndedAt = request.WorkEndedAt ?? currentTimeIst;
            request.CompletedAt = currentTimeIst;


            if (request.TotalPrice == 0 && request.Category != null)
            {
                request.TotalPrice = request.Category.BaseCharge;
            }

            await _billingService.CreateInvoiceAsync(request.Id);
        }
        else if (newStatus == RequestStatus.Cancelled)
        {
            request.TotalPrice = 0;
            request.WorkEndedAt = null;
            request.CompletedAt = null;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    
    public async Task<bool> RespondToAssignmentAsync(int requestId, string techId, bool accept, DateTime? plannedStartUtc = null)
    {
        var request = await _context.ServiceRequests.FindAsync(requestId);

        
        if (request == null || request.TechnicianId != techId) return false;

        if (accept)
        {
            request.Status = RequestStatus.Assigned; 
           
            if (plannedStartUtc.HasValue)
            {
                request.PlannedStartUtc = plannedStartUtc.Value.ToUniversalTime();
            }
        }
        else
        {
           
            request.TechnicianId = null;
            request.Status = RequestStatus.Requested;
            request.PlannedStartUtc = null;
            _notificationQueue.Enqueue(request.CustomerId, $"Appointment update: We are re-assigning a new technician to your request #{requestId}.");
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RescheduleRequestAsync(int requestId, DateTime newDate, string userId, string userRole)
    {
        var request = await _context.ServiceRequests.FindAsync(requestId);
        if (request == null) return false;

        if (userRole == "Customer" && request.CustomerId != userId) return false;

        
        request.ScheduledDate = newDate.ToUniversalTime();

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelRequestAsync(int requestId, string userId)
    {
        var request = await _context.ServiceRequests.FindAsync(requestId);
        if (request == null || request.CustomerId != userId) return false;

        
        if (request.Status == RequestStatus.InProgress) return false;

        var oldTechId = request.TechnicianId;
        request.Status = RequestStatus.Cancelled;
        await _context.SaveChangesAsync();
        if (!string.IsNullOrEmpty(oldTechId))
        {
            _notificationQueue.Enqueue(oldTechId, $"Task Cancelled: Request #{requestId} was cancelled by the customer.");
        }

        _notificationQueue.Enqueue(request.CustomerId, $"Your request #{requestId} has been successfully cancelled.");
        return true;
    }

    public async Task<PagedResponse<ServiceRequest>> GetCustomerRequestsAsync(string userId, QueryParameters query)
    {
        var baseQuery = _context.ServiceRequests
            .Include(r => r.Category).Include(r => r.Technician)
            .Where(r => r.CustomerId == userId);

        return await ApplyPagingAndSearch(baseQuery, query);
    }

    public async Task<PagedResponse<ServiceRequest>> GetTechnicianTasksAsync(string techId, QueryParameters query)
    {
        var baseQuery = _context.ServiceRequests
            .Include(r => r.Category).Include(r => r.Customer)
            .Where(r => r.TechnicianId == techId);

        return await ApplyPagingAndSearch(baseQuery, query);
    }

    public async Task<PagedResponse<ServiceRequest>> GetAllForMonitorAsync(QueryParameters query)
    {
        var baseQuery = _context.ServiceRequests
            .Include(r => r.Category).Include(r => r.Customer).Include(r => r.Technician);

        return await ApplyPagingAndSearch(baseQuery, query);
    }

   
    private async Task<PagedResponse<ServiceRequest>> ApplyPagingAndSearch(IQueryable<ServiceRequest> baseQuery, QueryParameters query)
    {
        
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.ToLower();
            baseQuery = baseQuery.Where(r =>
                r.IssueDescription.ToLower().Contains(search) ||
                // Check FullName OR UserName for Customer
                (r.Customer != null && (r.Customer.FullName.ToLower().Contains(search) || r.Customer.UserName.ToLower().Contains(search))) ||
                // Check FullName OR UserName for Technician
                (r.Technician != null && (r.Technician.FullName.ToLower().Contains(search) || r.Technician.UserName.ToLower().Contains(search))));
        }


        if (query.StatusFilter.HasValue)
        {
            baseQuery = baseQuery.Where(r => r.Status == query.StatusFilter.Value);
        }

        
        var totalCount = await baseQuery.CountAsync();

        
        var items = await baseQuery
            .OrderByDescending(r => r.CreatedAt)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();
       

        return new PagedResponse<ServiceRequest> { Items = items, TotalCount = totalCount };
    }

}