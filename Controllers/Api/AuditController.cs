using events_tickets.Contracts;
using events_tickets.Services;
using Microsoft.AspNetCore.Mvc;

namespace events_tickets.Controllers.Api;

[ApiController]
[Route("api/audit")]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpPost("sales")]
    public async Task<IActionResult> LogSale(SaleAuditRequest request, CancellationToken cancellationToken)
    {
        await _auditLogService.LogSaleAsync(request, HttpContext.TraceIdentifier, cancellationToken);

        return Accepted(new { traceId = HttpContext.TraceIdentifier, stored = "sales_logs" });
    }

    [HttpPost("tickets/prints")]
    public async Task<IActionResult> LogPrint(TicketPrintAuditRequest request, CancellationToken cancellationToken)
    {
        await _auditLogService.LogTicketPrintAsync(request, HttpContext.TraceIdentifier, false, cancellationToken);

        return Accepted(new { traceId = HttpContext.TraceIdentifier, stored = "ticket_logs" });
    }

    [HttpPost("tickets/reprints")]
    public async Task<IActionResult> LogReprint(TicketPrintAuditRequest request, CancellationToken cancellationToken)
    {
        await _auditLogService.LogTicketPrintAsync(request, HttpContext.TraceIdentifier, true, cancellationToken);

        return Accepted(new { traceId = HttpContext.TraceIdentifier, stored = "ticket_logs" });
    }

    [HttpPost("tickets/cancellations")]
    public async Task<IActionResult> LogCancellation(
        TicketCancellationAuditRequest request,
        CancellationToken cancellationToken)
    {
        await _auditLogService.LogTicketCancellationAsync(request, HttpContext.TraceIdentifier, cancellationToken);

        return Accepted(new { traceId = HttpContext.TraceIdentifier, stored = "ticket_logs" });
    }

    [HttpGet("tickets/{ticketId}")]
    public async Task<IActionResult> GetTicketTrace(string ticketId, CancellationToken cancellationToken)
    {
        var logs = await _auditLogService.GetTicketLogsAsync(ticketId, cancellationToken);

        return Ok(new { traceId = HttpContext.TraceIdentifier, ticketId, logs });
    }
}
