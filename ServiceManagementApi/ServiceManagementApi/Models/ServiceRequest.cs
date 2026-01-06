using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceManagementApi.Models;

public enum RequestStatus { Requested = 0, Assigned = 1, InProgress = 2, Completed = 3, Cancelled = 4, Closed = 5 }
public enum Priority { Low, Medium, High }

public class ServiceRequest
{
    public int Id { get; set; }
    public required string IssueDescription { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Requested;
    public Priority Priority { get; set; }
    public DateTime ScheduledDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public double? EstimatedDurationHours { get; set; }
    public DateTime? PlannedStartUtc { get; set; }
    public DateTime? WorkStartedAt { get; set; }
    public DateTime? WorkEndedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    public int CategoryId { get; set; }
    public virtual ServiceCategory Category { get; set; } = null!;

    public required string CustomerId { get; set; }
    [ForeignKey("CustomerId")]
    public virtual ApplicationUser Customer { get; set; } = null!;

    public string? TechnicianId { get; set; }
    [ForeignKey("TechnicianId")]
    public virtual ApplicationUser? Technician { get; set; }

    public string? ResolutionNotes { get; set; }
}