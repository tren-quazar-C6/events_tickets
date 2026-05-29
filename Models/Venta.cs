namespace events_tickets.Models;

public enum EstadoVenta { pendiente, completada, cancelada, reembolsada }

public class Venta
{
    public int IdVenta { get; set; }
    public int IdEvento { get; set; }
    public int IdCliente { get; set; }
    public int IdStaff { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "completada";
    public string? Notas { get; set; }
    public DateTime FechaVenta { get; set; } = DateTime.UtcNow;
    public DateTime? FechaCancelacion { get; set; }
}