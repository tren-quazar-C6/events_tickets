using events_tickets.Contracts;
using events_tickets.Infrastructure;
using events_tickets.Services;
using Microsoft.AspNetCore.Mvc;

namespace events_tickets.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class VentasController : ControllerBase
{
    private readonly IVentaService _svc;
    public VentasController(IVentaService svc) => _svc = svc;

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearVentaRequest req)
    {
        try
        {
            var venta = await _svc.CrearAsync(req);
            return CreatedAtAction(nameof(Obtener), new { id = venta.IdVenta },
                ServiceResponse<object>.Ok(venta));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ServiceResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        var venta = await _svc.ObtenerAsync(id);
        return venta == null
            ? NotFound(ServiceResponse<object>.Fail("Venta no encontrada"))
            : Ok(ServiceResponse<object>.Ok(venta));
    }

    [HttpGet("cliente/{idCliente:int}")]
    public async Task<IActionResult> PorCliente(int idCliente) =>
        Ok(ServiceResponse<object>.Ok(await _svc.ObtenerPorClienteAsync(idCliente)));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarVentaRequest req)
    {
        var venta = await _svc.CancelarAsync(id, req.Motivo);
        return venta == null
            ? NotFound(ServiceResponse<object>.Fail("Venta no encontrada"))
            : Ok(ServiceResponse<object>.Ok(venta));
    }
}
