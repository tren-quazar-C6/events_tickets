namespace events_tickets.Models;

public class EventoResumen
{
    public int IdEvento { get; set; }
    public string? NombreEvento { get; set; }
    public string? Descripcion { get; set; }
    public DateTime FechaEvento { get; set; }
    public DateTime FechaInicioVentas { get; set; }
    public DateTime FechaFinVentas { get; set; }
    public int CapacidadTotal { get; set; }
    public string? TipoEvento { get; set; }
    public string? ImagenPrincipal { get; set; }
    public int AsientosDisponibles { get; set; }
    public double? PrecioDesde { get; set; }
}