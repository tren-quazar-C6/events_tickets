namespace events_tickets.Contracts;

public record CreateEventRequest(
    string Name,
    string Description,
    DateTime Date,
    string Venue,
    int TotalSeats,
    decimal PricePerSeat
);

public record SeatDefinition(string SeatNumber, string Section);