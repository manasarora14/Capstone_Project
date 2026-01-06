using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ServiceManagementApi.Models;
using Microsoft.AspNetCore.Identity;

namespace ServiceManagementApi.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    
    public DbSet<ServiceRequest> ServiceRequests { get; set; }
    public DbSet<ServiceCategory> ServiceCategories { get; set; }
    public DbSet<Invoice> Invoices { get; set; }

   

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        
        builder.Entity<Invoice>()
            .Property(i => i.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Entity<ServiceCategory>()
            .Property(s => s.BaseCharge)
            .HasColumnType("decimal(18,2)");

        
        builder.Entity<ServiceRequest>()
            .HasOne(sr => sr.Category)
            .WithMany()
            .HasForeignKey(sr => sr.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ServiceRequest>()
            .HasOne(sr => sr.Customer)
            .WithMany()
            .HasForeignKey(sr => sr.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ServiceRequest>()
            .HasOne(sr => sr.Technician)
            .WithMany()
            .HasForeignKey(sr => sr.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);

        
        builder.Entity<Invoice>()
            .HasOne(i => i.ServiceRequest)
            .WithMany()
            .HasForeignKey(i => i.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}