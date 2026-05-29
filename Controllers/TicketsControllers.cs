using events_tickets.Contracts;
using events_tickets.Services;
using Microsoft.AspNetCore.Mvc;

namespace events_tickets.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _tickets;
    private readonly IPrintService _print;
    private readonly IEventService _events;
    private readonly IClienteService _clientes;

    public TicketsController(ITicketService tickets, IPrintService print,
        IEventService events, IClienteService clientes)
    {
        _tickets   = tickets;
        _print     = print;
        _events    = events;
        _clientes = clientes;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var t = await _tickets.GetAsync(id);
        return t == null ? NotFound() : Ok(t);
    }

    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var t = await _tickets.GetByCodeAsync(code);
        return t == null ? NotFound() : Ok(t);
    }

    [HttpGet("sale/{saleId}")]
    public async Task<IActionResult> GetBySale(string saleId) =>
        Ok(await _tickets.GetBySaleAsync(saleId));

    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] ValidateTicketRequest req)
    {
        var t = await _tickets.ValidateAsync(req);
        return t == null
            ? Conflict(new { error = "Ticket no encontrado o ya utilizado" })
            : Ok(t);
    }

    [HttpGet("{id}/qr")]
    public async Task<IActionResult> Qr(string id)
    {
        var t = await _tickets.GetAsync(id);
        if (t == null) return NotFound();
        return File(Convert.FromBase64String(t.QrCodeBase64), "image/png");
    }

    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> Pdf(string id)
    {
        try
        {
            var bytes = await _tickets.GeneratePdfAsync(id);
            return File(bytes, "application/pdf", $"ticket-{id}.pdf");
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id}/print")]
    public async Task<IActionResult> Print(string id)
    {
        var ticket = await _tickets.GetAsync(id);
        if (ticket == null) return NotFound();

        var ev = await _events.GetAsync(ticket.EventId);
        var customer = await _clientes.GetAsync(ticket.CustomerId);
        if (ev == null || customer == null)
            return BadRequest(new { error = "Datos de evento o cliente no encontrados" });

        var ok = await _print.PrintAsync(ticket, ev, customer);
        return ok ? Ok(new { printed = true }) : StatusCode(500, new { error = "Error al imprimir" });
    }
}