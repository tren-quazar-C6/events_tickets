namespace events_tickets.Models;

public class Cliente
{
    public int IdCliente { get; set; }
    public string Nombre { get; set; } = "";
    public string NumeroDocumento { get; set; } = "";
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}