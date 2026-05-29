namespace events_tickets.Dtos;

public class TicketResumenDto
{
    public int IdTicket { get; set; }
    public string CodigoUnico { get; set; } = "";
    public string QrToken { get; set; } = "";
    public string? CodigoAsiento { get; set; }
    public string? Zona { get; set; }
    public decimal PrecioPagado { get; set; }
    public string EstadoTicket { get; set; } = "";
    public DateTime FechaEmision { get; set; }
}

public class TicketDetalleDto : TicketResumenDto
{
    public int IdEvento { get; set; }
    public string? NombreEvento { get; set; }
    public DateTime? FechaEvento { get; set; }
    public int IdCliente { get; set; }
    public string? NombreCliente { get; set; }
}