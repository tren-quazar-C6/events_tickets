namespace events_tickets.Models;

public class Event
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime Date { get; set; }
    public string Venue { get; set; } = "";
    public int TotalSeats { get; set; }
    public decimal PricePerSeat { get; set; }
    public bool IsActive { get; set; } = true;
}
