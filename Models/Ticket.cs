namespace events_tickets.Models;

public class Ticket
{
    public int IdTicket { get; set; }
    public int IdVenta { get; set; }
    public int IdEvento { get; set; }
    public int IdCliente { get; set; }
    public int IdEventoAsiento { get; set; }
    public string? CodigoAsiento { get; set; }
    public string? Zona { get; set; }
    public string CodigoUnico { get; set; } = "";
    public string QrToken { get; set; } = "";
    public string? QrImagenBase64 { get; set; }
    public decimal PrecioPagado { get; set; }
    public string EstadoTicket { get; set; } = "activo";
    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;
    public DateTime? FechaValidacion { get; set; }
    public int? IdStaffValidacion { get; set; }
}