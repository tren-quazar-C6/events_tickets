namespace events_tickets.Dtos;

public class VentaResumenDto
{
    public int IdVenta { get; set; }
    public int IdEvento { get; set; }
    public string? NombreEvento { get; set; }
    public int IdCliente { get; set; }
    public string? NombreCliente { get; set; }
    public int IdStaff { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "";
    public DateTime FechaVenta { get; set; }
    public int CantidadTickets { get; set; }
}

public class VentaDetalleDto : VentaResumenDto
{
    public List<TicketResumenDto> Tickets { get; set; } = new();
}