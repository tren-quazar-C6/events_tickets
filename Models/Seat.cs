namespace events_tickets.Models;

public class Seat
{
    public string Id { get; set; } = "";
    public string EventId { get; set; } = "";
    public string SeatNumber { get; set; } = "";
    public string Section { get; set; } = "";
    public bool IsAvailable { get; set; } = true;
}
