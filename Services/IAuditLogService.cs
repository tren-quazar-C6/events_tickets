using events_tickets.Contracts;
using events_tickets.Models;

namespace events_tickets.Services;

public interface IAuditLogService
{
    Task LogSaleAsync(SaleAuditRequest request, string traceId, CancellationToken cancellationToken);

    Task LogTicketPrintAsync(TicketPrintAuditRequest request, string traceId, bool isReprint, CancellationToken cancellationToken);

    Task LogTicketCancellationAsync(TicketCancellationAuditRequest request, string traceId, CancellationToken cancellationToken);

    Task LogSystemErrorAsync(SystemErrorEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TicketLogEntry>> GetTicketLogsAsync(string ticketId, CancellationToken cancellationToken);
}
