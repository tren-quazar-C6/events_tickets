using System.Data;
using events_tickets.Dtos;
using events_tickets.Models;

namespace events_tickets.Services;

public interface ITicketService
{
    Task<List<Ticket>> GenerarAsync(Venta venta, List<AsientoInfo> asientos);
    Task<List<Ticket>> GenerarAsync(Venta venta, List<AsientoInfo> asientos, IDbConnection conn, IDbTransaction tx);
    Task<TicketDetalleDto?> ObtenerAsync(int id);
    Task<TicketDetalleDto?> ObtenerPorCodigoAsync(string codigoOQrToken);
    Task<List<TicketResumenDto>> ObtenerPorVentaAsync(int idVenta);
    Task<List<TicketResumenDto>> ObtenerPorClienteAsync(int idCliente);
    Task<TicketDetalleDto?> ValidarAsync(string codigoOQrToken, int idStaff);
    Task<byte[]> GenerarPdfAsync(int idTicket);
}

public record AsientoInfo(int IdEventoAsiento, string CodigoAsiento, string Zona, decimal Precio);
