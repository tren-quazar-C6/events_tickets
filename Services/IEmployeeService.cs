using events_tickets.Contracts;
using events_tickets.Models;

namespace events_tickets.Services;

public interface IEmployeeService
{
    Task<Employee> CreateAsync(CreateEmployeeRequest req);
    Task<Employee?> GetAsync(string id);
    Task<List<Employee>> GetActiveAsync();
    Task<Employee?> LoginAsync(string email, string password);
}
