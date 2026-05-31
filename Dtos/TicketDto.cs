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
    public string? QrImagenBase64 { get; set; }
    
    // Propiedades para la vista Print
    public string? EventName { get; set; }
    public DateTime? EventDate { get; set; }
    
    // Propiedades computed
    public string SeatInfo => !string.IsNullOrEmpty(CodigoAsiento) && !string.IsNullOrEmpty(Zona)
        ? $"{Zona} - {CodigoAsiento}"
        : "N/A";
    public string Code => CodigoUnico;
    public string QrCode => QrImagenBase64 ?? "";
    public decimal Price => PrecioPagado;
}
public class TicketDetalleDto : TicketResumenDto
{
    public int IdEvento { get; set; }
    public string? NombreEvento { get; set; }
    public DateTime? FechaEvento { get; set; }
    public int IdCliente { get; set; }
    public string? NombreCliente { get; set; }
}
