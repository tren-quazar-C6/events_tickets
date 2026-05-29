using events_tickets.Contracts;
using events_tickets.Services;
using Microsoft.AspNetCore.Mvc;

namespace events_tickets.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _svc;
    public EventsController(IEventService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetActive() => Ok(await _svc.GetActiveAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var ev = await _svc.GetAsync(id);
        return ev == null ? NotFound() : Ok(ev);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest req)
    {
        var ev = await _svc.CreateAsync(req);
        return CreatedAtAction(nameof(Get), new { id = ev.Id }, ev);
    }

    [HttpPost("{id}/seats")]
    public async Task<IActionResult> CreateSeats(string id, [FromBody] List<SeatDefinition> seats) =>
        Ok(await _svc.CreateSeatsAsync(id, seats));

    [HttpGet("{id}/seats/available")]
    public async Task<IActionResult> AvailableSeats(string id) =>
        Ok(await _svc.GetAvailableSeatsAsync(id));
}