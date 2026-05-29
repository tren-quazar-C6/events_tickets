namespace events_tickets.Contracts;

public record ValidateTicketRequest(string TicketCode, string EmployeeId);