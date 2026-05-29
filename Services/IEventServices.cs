using events_tickets.Contracts;
using events_tickets.Models;

namespace events_tickets.Services;

public interface IEventService
{
    Task<Event> CreateAsync(CreateEventRequest req);
    Task<Event?> GetAsync(string id);
    Task<List<Event>> GetActiveAsync();
    Task<List<Seat>> CreateSeatsAsync(string eventId, List<SeatDefinition> seats);
    Task<List<Seat>> GetAvailableSeatsAsync(string eventId);
}