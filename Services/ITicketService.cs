using events_tickets.Dtos;
using events_tickets.Models;

namespace events_tickets.Services;

public interface ITicketService
{
    Task<List<Ticket>> GenerarAsync(Venta venta, List<AsientoInfo> asientos);
    Task<TicketDetalleDto?> ObtenerAsync(int id);
    Task<List<TicketResumenDto>> ObtenerPorVentaAsync(int idVenta);
    Task<List<TicketResumenDto>> ObtenerPorClienteAsync(int idCliente);
    Task<byte[]> GenerarPdfAsync(int idTicket);
}

public record AsientoInfo(int IdEventoAsiento, string CodigoAsiento, string Zona, decimal Precio);