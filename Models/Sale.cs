namespace events_tickets.Models;

public class Sale
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public Customer Customer { get; set; } = new();
    public int Quantity { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<Ticket> Tickets { get; set; } = new();
}