using events_tickets.Contracts;
using events_tickets.Models;

namespace events_tickets.Services;

public interface IVentaService
{
    Task<(Sale sale, List<Ticket> tickets)> CreateAsync(CreateSaleRequest req);
    Task<Sale?> GetAsync(string id);
    Task<List<Sale>> GetByCustomerAsync(string customerId);
    Task<Sale?> CancelAsync(string id, string reason);
}