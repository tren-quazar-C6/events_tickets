namespace events_tickets.Contracts;

public record CreateEmployeeRequest(
    string FullName,
    string DocumentNumber,
    string Position
);