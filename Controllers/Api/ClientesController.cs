using events_tickets.Contracts;
using events_tickets.Services;
using Microsoft.AspNetCore.Mvc;

namespace events_tickets.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _clientes;
    private readonly ITicketService _tickets;

    public ClientesController(IClienteService clientes, ITicketService tickets)
    {
        _clientes = clientes;
        _tickets = tickets;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _clientes.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var c = await _clientes.GetAsync(id);
        return c == null ? NotFound() : Ok(c);
    }

    [HttpGet("document/{doc}")]
    public async Task<IActionResult> GetByDocument(string doc)
    {
        var c = await _clientes.GetByDocumentAsync(doc);
        return c == null ? NotFound() : Ok(c);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest req)
    {
        var c = await _clientes.CreateAsync(req);
        return CreatedAtAction(nameof(Get), new { id = c.Id }, c);
    }

    // Portal público: historial de tickets del cliente
    [HttpGet("{id}/tickets")]
    public async Task<IActionResult> GetTickets(string id) =>
        Ok(await _tickets.GetByCustomerAsync(id));
}