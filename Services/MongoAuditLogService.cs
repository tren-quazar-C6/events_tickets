using events_tickets.Configuration;
using events_tickets.Contracts;
using events_tickets.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace events_tickets.Services;

public sealed class MongoAuditLogService : IAuditLogService
{
    private readonly IMongoCollection<SaleLogEntry> _salesLogs;
    private readonly IMongoCollection<TicketLogEntry> _ticketLogs;
    private readonly IMongoCollection<EmployeeActionEntry> _employeeActions;
    private readonly IMongoCollection<SystemErrorEntry> _systemErrors;

    public MongoAuditLogService(IMongoClient mongoClient, IOptions<MongoLoggingOptions> options)
    {
        var loggingOptions = options.Value;
        var database = mongoClient.GetDatabase(loggingOptions.DatabaseName);

        _salesLogs = database.GetCollection<SaleLogEntry>(loggingOptions.SalesLogsCollection);
        _ticketLogs = database.GetCollection<TicketLogEntry>(loggingOptions.TicketLogsCollection);
        _employeeActions = database.GetCollection<EmployeeActionEntry>(loggingOptions.EmployeeActionsCollection);
        _systemErrors = database.GetCollection<SystemErrorEntry>(loggingOptions.SystemErrorsCollection);
    }

    public async Task LogSaleAsync(SaleAuditRequest request, string traceId, CancellationToken cancellationToken)
    {
        await _salesLogs.InsertOneAsync(new SaleLogEntry
        {
            TraceId = traceId,
            EmployeeId = request.EmployeeId,
            EmployeeName = request.EmployeeName,
            TicketId = request.TicketId,
            EventId = request.EventId,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod
        }, cancellationToken: cancellationToken);

        await _employeeActions.InsertOneAsync(new EmployeeActionEntry
        {
            TraceId = traceId,
            EmployeeId = request.EmployeeId,
            Action = "ticket_sold",
            ResourceType = "ticket",
            ResourceId = request.TicketId,
            Notes = $"Event={request.EventId}; Amount={request.Amount}"
        }, cancellationToken: cancellationToken);
    }

    public async Task LogTicketPrintAsync(
        TicketPrintAuditRequest request,
        string traceId,
        bool isReprint,
        CancellationToken cancellationToken)
    {
        await _ticketLogs.InsertOneAsync(new TicketLogEntry
        {
            TraceId = traceId,
            EmployeeId = request.EmployeeId,
            TicketId = request.TicketId,
            Action = isReprint ? "ticket_reprinted" : "ticket_printed",
            PrinterName = request.PrinterName,
            Reason = request.Reason
        }, cancellationToken: cancellationToken);

        await _employeeActions.InsertOneAsync(new EmployeeActionEntry
        {
            TraceId = traceId,
            EmployeeId = request.EmployeeId,
            Action = isReprint ? "reprint_ticket" : "print_ticket",
            ResourceType = "ticket",
            ResourceId = request.TicketId,
            Notes = request.Reason
        }, cancellationToken: cancellationToken);
    }

    public async Task LogTicketCancellationAsync(
        TicketCancellationAuditRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        await _ticketLogs.InsertOneAsync(new TicketLogEntry
        {
            TraceId = traceId,
            EmployeeId = request.EmployeeId,
            TicketId = request.TicketId,
            Action = "ticket_cancelled",
            Reason = request.Reason
        }, cancellationToken: cancellationToken);

        await _employeeActions.InsertOneAsync(new EmployeeActionEntry
        {
            TraceId = traceId,
            EmployeeId = request.EmployeeId,
            Action = "cancel_ticket",
            ResourceType = "ticket",
            ResourceId = request.TicketId,
            Notes = request.Reason
        }, cancellationToken: cancellationToken);
    }

    public Task LogSystemErrorAsync(SystemErrorEntry entry, CancellationToken cancellationToken)
    {
        return _systemErrors.InsertOneAsync(entry, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyCollection<TicketLogEntry>> GetTicketLogsAsync(
        string ticketId,
        CancellationToken cancellationToken)
    {
        return await _ticketLogs
            .Find(log => log.TicketId == ticketId)
            .SortByDescending(log => log.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
