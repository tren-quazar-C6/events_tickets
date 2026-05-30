namespace events_tickets.Models;

public class EventoDetalle : EventoResumen
{
    public int AsientosReservados { get; set; }
    public int AsientosVendidos { get; set; }
    public List<ImagenEvento> Imagenes { get; set; } = new();
}

public class ImagenEvento
{
    public int IdImagen { get; set; }
    public string? RutaUrl { get; set; }
    public bool Principal { get; set; }
}