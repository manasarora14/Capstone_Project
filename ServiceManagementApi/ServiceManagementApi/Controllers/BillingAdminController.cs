using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceManagementApi.Services;

namespace ServiceManagementApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Manager")]
public class BillingAdminController : ControllerBase
{
    private readonly IBillingService _billingService;

    public BillingAdminController(IBillingService billingService) => _billingService = billingService;

    [HttpPost("repair-invoices")]
    public async Task<IActionResult> RepairInvoices()
    {
        var fixedCount = await _billingService.RepairMissingInvoicesAsync();
        return Ok(new { fixedCount });
    }
}
