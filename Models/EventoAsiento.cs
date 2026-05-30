namespace events_tickets.Models;

public class EventoAsiento
{
    public int IdEventoAsiento { get; set; }
    public int IdAsiento { get; set; }
    public string? CodigoAsiento { get; set; }
    public string? Fila { get; set; }
    public int Numero { get; set; }
    public int IdZona { get; set; }
    public string? Zona { get; set; }
    public string? ColorZona { get; set; }
    public double Precio { get; set; }
    public string? Estado { get; set; }
}