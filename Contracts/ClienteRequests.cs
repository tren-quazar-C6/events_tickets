namespace events_tickets.Contracts;

public record CreateCustomerRequest(
    string FullName,
    string DocumentNumber,
    string Email,
    string Phone
);