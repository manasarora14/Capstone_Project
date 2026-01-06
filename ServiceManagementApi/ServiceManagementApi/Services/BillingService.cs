using Microsoft.EntityFrameworkCore;
using ServiceManagementApi.Data;
using ServiceManagementApi.Models;
using System.Security.Claims;

namespace ServiceManagementApi.Services
{
    public interface IBillingService
    {
        Task<List<Invoice>> GetInvoicesAsync(string userId, string userRole);
        Task<bool> PayInvoiceAsync(int invoiceId);
        Task<bool> PayInvoiceAsync(int invoiceId, string paidByUserId);
        Task CreateInvoiceAsync(int requestId); 

       
        Task<int> RepairMissingInvoicesAsync();
    }

    
    public class BillingService : IBillingService
    {
        private readonly ApplicationDbContext _context;

        public BillingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Invoice>> GetInvoicesAsync(string userId, string userRole)
        {
            var query = _context.Invoices.Include(i => i.ServiceRequest).AsQueryable();

            if (userRole == "Customer")
            {
                query = query.Where(i => i.ServiceRequest!.CustomerId == userId);
            }

            return await query.OrderByDescending(i => i.GeneratedDate).ToListAsync();
        }

        public async Task<bool> PayInvoiceAsync(int invoiceId, string paidByUserId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.ServiceRequest)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null) return false;

            invoice.Status = "Paid";
            invoice.PaidAt = DateTime.UtcNow;
            invoice.PaidBy = paidByUserId;

            if (invoice.ServiceRequest != null)
            {
         
                invoice.ServiceRequest.Status = RequestStatus.Closed;

                if (!invoice.ServiceRequest.CompletedAt.HasValue)
                {
                    invoice.ServiceRequest.CompletedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

     
        public async Task<bool> PayInvoiceAsync(int invoiceId)
        {
           
            return await PayInvoiceAsync(invoiceId, string.Empty);
        }

        public async Task CreateInvoiceAsync(int requestId)
        {
            var request = await _context.ServiceRequests
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request != null && request.Category != null && request.Status != RequestStatus.Cancelled)
            {
                var exists = await _context.Invoices.AnyAsync(i => i.ServiceRequestId == requestId);
                if (!exists)
                {
                   
                    var invoiceAmount = request.TotalPrice > 0m ? request.TotalPrice : request.Category.BaseCharge;

                    var invoice = new Invoice
                    {
                        ServiceRequestId = requestId,
                        Amount = invoiceAmount,
                        Status = "Pending",
                        GeneratedDate = DateTime.UtcNow
                    };
                    _context.Invoices.Add(invoice);
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task<int> RepairMissingInvoicesAsync()
        {
            var completedRequests = await _context.ServiceRequests
                .Include(r => r.Category)
                .Where(r => r.Status == RequestStatus.Completed || r.Status == RequestStatus.Closed)
                .ToListAsync();

            int fixedCount = 0;

            foreach (var req in completedRequests)
            {
       
                if ((req.TotalPrice == 0m || req.TotalPrice == decimal.Zero) && req.Category != null)
                {
                    req.TotalPrice = req.Category.BaseCharge; 
                    fixedCount++;
                }

                var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.ServiceRequestId == req.Id);
                if (invoice == null)
                {
                    var inv = new Invoice
                    {
                        ServiceRequestId = req.Id,
                        Amount = req.TotalPrice != 0m ? req.TotalPrice : (req.Category?.BaseCharge ?? 0m),
                        Status = "Pending",
                        GeneratedDate = DateTime.UtcNow
                    };
                    _context.Invoices.Add(inv);
                    fixedCount++;
                }
                else
                {
                    var expectedAmount = req.TotalPrice != 0m ? req.TotalPrice : (req.Category?.BaseCharge ?? 0m);
                    if (invoice.Amount != expectedAmount)
                    {
                        invoice.Amount = expectedAmount;
                        fixedCount++;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return fixedCount;
        }
    }
}