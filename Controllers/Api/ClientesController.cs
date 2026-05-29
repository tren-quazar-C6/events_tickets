using events_tickets.Contracts;
using events_tickets.Infrastructure;
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
    public async Task<IActionResult> Listar() =>
        Ok(ServiceResponse<object>.Ok(await _clientes.ListarAsync()));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        var c = await _clientes.ObtenerAsync(id);
        return c == null
            ? NotFound(ServiceResponse<object>.Fail("Cliente no encontrado"))
            : Ok(ServiceResponse<object>.Ok(c));
    }

    [HttpGet("documento/{doc}")]
    public async Task<IActionResult> PorDocumento(string doc)
    {
        var c = await _clientes.ObtenerPorDocumentoAsync(doc);
        return c == null
            ? NotFound(ServiceResponse<object>.Fail("Cliente no encontrado"))
            : Ok(ServiceResponse<object>.Ok(c));
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearClienteRequest req)
    {
        var c = await _clientes.CrearAsync(req);
        return CreatedAtAction(nameof(Obtener), new { id = c.IdCliente },
            ServiceResponse<object>.Ok(c));
    }

    // Portal público: historial de tickets del cliente
    [HttpGet("{id:int}/tickets")]
    public async Task<IActionResult> MisTickets(int id) =>
        Ok(ServiceResponse<object>.Ok(await _tickets.ObtenerPorClienteAsync(id)));
}
