namespace ServiceManagementApi.DTOs;

public class AvailabilityDto
{
    public string TechnicianId { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
}

public class ScheduleVisitDto
{
    public int ServiceRequestId { get; set; }
    public string TechnicianId { get; set; } = string.Empty;
    public DateTime ScheduledUtc { get; set; }
}

public class TechnicianAvailabilityResponseDto
{
    public int Id { get; set; }
    public string TechnicianId { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
}

public class TechnicianTaskDto
{
    public int RequestId { get; set; }
    public string IssueDescription { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
}

public class AvailableTechDto
{
    public string TechnicianId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public DateTime? NextAvailableFromUtc { get; set; }
}

public class TechnicianWorkloadDto
{
    public string TechnicianId { get; set; } = string.Empty;
    public double TotalHoursWorked { get; set; }
    public decimal TotalEarnings { get; set; }
    public class FinishWorkDto
    {
        public string Notes { get; set; } = string.Empty;
    }


    public List<object>? MonthlyEarnings { get; set; }

    public List<TechnicianTaskDto>? PreviousTasks { get; set; }
}
