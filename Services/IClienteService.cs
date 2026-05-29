using events_tickets.Contracts;
using events_tickets.Models;

namespace events_tickets.Services;

public interface IClienteService
{
    Task<Customer> CreateAsync(CreateCustomerRequest req);
    Task<Customer?> GetAsync(string id);
    Task<Customer?> GetByDocumentAsync(string documentNumber);
    Task<List<Customer>> GetAllAsync();
}