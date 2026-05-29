using events_tickets.Models;

namespace events_tickets.Services;

public interface IPrintService
{
    Task<bool> ImprimirAsync(Ticket ticket, string nombreEvento, DateTime fechaEvento, string nombreCliente, string numeroDocumento);
}