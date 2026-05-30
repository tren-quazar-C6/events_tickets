namespace events_tickets.Models;

public class SaleViewModel
{
    public int EventoId { get; set; }
    public EventoDetalle? Evento { get; set; }
    public List<EventoAsiento> AsientosDisponibles { get; set; } = new();
    public List<int> AsientosSeleccionados { get; set; } = new();
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string CustomerDocument { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
}