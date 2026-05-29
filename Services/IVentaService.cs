using events_tickets.Contracts;
using events_tickets.Dtos;

namespace events_tickets.Services;

public interface IVentaService
{
    Task<VentaDetalleDto> CrearAsync(CrearVentaRequest req);
    Task<VentaDetalleDto?> ObtenerAsync(int id);
    Task<List<VentaResumenDto>> ObtenerPorClienteAsync(int idCliente);
    Task<VentaResumenDto?> CancelarAsync(int id, string motivo);
}
