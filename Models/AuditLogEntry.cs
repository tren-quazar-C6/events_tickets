using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace events_tickets.Models;

public sealed class SaleLogEntry
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; init; }

    public required string TraceId { get; init; }

    public required string EmployeeId { get; init; }

    public required string EmployeeName { get; init; }

    public required string TicketId { get; init; }

    public required string EventId { get; init; }

    public decimal Amount { get; init; }

    public string? PaymentMethod { get; init; }

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}

public sealed class TicketLogEntry
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; init; }

    public required string TraceId { get; init; }

    public required string EmployeeId { get; init; }

    public required string TicketId { get; init; }

    public required string Action { get; init; }

    public string? PrinterName { get; init; }

    public string? Reason { get; init; }

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}

public sealed class EmployeeActionEntry
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; init; }

    public required string TraceId { get; init; }

    public required string EmployeeId { get; init; }

    public required string Action { get; init; }

    public required string ResourceType { get; init; }

    public required string ResourceId { get; init; }

    public string? Notes { get; init; }

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}

public sealed class SystemErrorEntry
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; init; }

    public required string TraceId { get; init; }

    public required string Path { get; init; }

    public required string Method { get; init; }

    public required string ErrorType { get; init; }

    public required string Message { get; init; }

    public string? StackTrace { get; init; }

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
