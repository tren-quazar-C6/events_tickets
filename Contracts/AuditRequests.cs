using System.ComponentModel.DataAnnotations;

namespace events_tickets.Contracts;

public sealed record SaleAuditRequest(
    [Required] string EmployeeId,
    [Required] string EmployeeName,
    [Required] string TicketId,
    [Required] string EventId,
    decimal Amount,
    string? PaymentMethod);

public sealed record TicketPrintAuditRequest(
    [Required] string EmployeeId,
    [Required] string TicketId,
    [Required] string PrinterName,
    string? Reason);

public sealed record TicketCancellationAuditRequest(
    [Required] string EmployeeId,
    [Required] string TicketId,
    [Required] string Reason);
