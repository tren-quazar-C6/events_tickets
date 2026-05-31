using events_tickets.Dtos;

namespace events_tickets.Services;

public interface IEmailService
{
    Task<bool> SendTicketsAsync(VentaDetalleDto venta);
}
