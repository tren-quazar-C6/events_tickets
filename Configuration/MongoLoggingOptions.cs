namespace events_tickets.Configuration;

public sealed class MongoLoggingOptions
{
    public const string SectionName = "MongoDB"; 

    public string ConnectionString { get; init; } = "mongodb://localhost:27017";

    public string DatabaseName { get; init; } = "events_logs"; 

    public string SalesLogsCollection { get; init; } = "sales_logs";

    public string TicketLogsCollection { get; init; } = "ticket_logs";

    public string EmployeeActionsCollection { get; init; } = "employee_actions";

    public string SystemErrorsCollection { get; init; } = "system_errors";
}