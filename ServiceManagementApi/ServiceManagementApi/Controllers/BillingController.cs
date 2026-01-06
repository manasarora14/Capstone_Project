using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceManagementApi.Services;
using System.Security.Claims;

namespace ServiceManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillingController : ControllerBase
{
    private readonly IBillingService _billingService;

    public BillingController(IBillingService billingService)
    {
        _billingService = billingService;
    }

    [HttpGet("my-invoices")]
    [Authorize]
    public async Task<IActionResult> GetMyInvoices()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var invoices = await _billingService.GetInvoicesAsync(userId, userRole);
        return Ok(invoices);
    }

    [HttpPost("pay/{id}")]
    [Authorize(Roles = "Customer,Manager")]
    public async Task<IActionResult> Pay(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ok = await _billingService.PayInvoiceAsync(id, userId!);
        return ok ? Ok(new { message = "Payment successful!" }) : BadRequest();
    }

    
    [NonAction]
    public async Task<IActionResult> PayInvoice(int invoiceId)
    {
        var ok = await _billingService.PayInvoiceAsync(invoiceId);
        return ok ? (IActionResult)Ok(new { message = "Payment successful!" }) : NotFound();
    }
}