using System.ComponentModel.DataAnnotations;

namespace ServiceManagementApi.Models;

public class Invoice
{
    public int Id { get; set; }

    [Required]
    public int ServiceRequestId { get; set; }
    public ServiceRequest? ServiceRequest { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }

    
    public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;

   
    public string Status { get; set; } = "Pending";

    
    public DateTime? PaidAt { get; set; }
    public string? PaidBy { get; set; }
}