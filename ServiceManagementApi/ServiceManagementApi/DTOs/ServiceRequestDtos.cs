using ServiceManagementApi.Models;

namespace ServiceManagementApi.DTOs
{
    public class CreateRequestDto
    {
        public string IssueDescription { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public Priority Priority { get; set; }
        public DateTime ScheduledDate { get; set; }
        
        public TimeSpan? ScheduledTime { get; set; }
    }

    public class UpdateStatusDto
    {
        public int RequestId { get; set; }
        public RequestStatus Status { get; set; }
        public string? ResolutionNotes { get; set; } 
    }

    public class AssignTechnicianDto
    {
        public int RequestId { get; set; }
        public string TechnicianId { get; set; } = string.Empty;
    }

    public class RespondAssignmentDto
    {
        public int RequestId { get; set; }
        public bool Accept { get; set; }
        public DateTime? PlannedStartUtc { get; set; }
    }

    
    public class CreateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal BaseCharge { get; set; }
        public int SlaHours { get; set; }
    }

    public class UpdateCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal BaseCharge { get; set; }
        public int SlaHours { get; set; }
    }

    
    public class RescheduleDto
    {
        public int Id { get; set; }
        public DateTime NewDate { get; set; }
    }

    
    public class RescheduleByPartsDto
    {
        public int Id { get; set; }
        
        public string? Date { get; set; }
       
        public string? Time { get; set; }
    }

    
    public class ServiceRequestResponseDto
    {
        public int Id { get; set; }
        public RequestStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty; 

        public Priority Priority { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string? TechnicianId { get; set; }
        public int CategoryId { get; set; }

        public string? CustomerName { get; set; }
        public string? TechnicianName { get; set; }

        
        public string IssueDescription { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public decimal TotalPrice { get; set; }
        

        
        public DateTime? WorkStartedAt { get; set; }
        public DateTime? WorkEndedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Duration { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int SlaHours { get; set; }
        public string? ResolutionNotes { get; set; }
    }
}