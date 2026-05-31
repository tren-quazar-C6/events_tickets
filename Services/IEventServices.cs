using events_tickets.Contracts;
using events_tickets.Models;

namespace events_tickets.Services;

public interface IEventService
{
    Task<EventoDetalle> CreateAsync(CreateEventRequest req);
    Task<EventoDetalle?> GetAsync(int id);
    Task<List<EventoResumen>> GetActiveAsync();
    Task<List<EventoAsiento>> CreateSeatsAsync(int eventId, List<SeatDefinition> seats);
    Task<List<EventoAsiento>> GetAvailableSeatsAsync(int eventId);
}
