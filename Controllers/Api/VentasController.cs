using events_tickets.Contracts;
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
    public async Task<IActionResult> Create([FromBody] CreateSaleRequest req)
    {
        try
        {
            var (sale, tickets) = await _svc.CreateAsync(req);
            return Ok(new { sale, tickets });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var s = await _svc.GetAsync(id);
        return s == null ? NotFound() : Ok(s);
    }

    [HttpGet("customer/{customerId}")]
    public async Task<IActionResult> GetByCustomer(string customerId) =>
        Ok(await _svc.GetByCustomerAsync(customerId));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(string id, [FromBody] CancelSaleRequest req)
    {
        var s = await _svc.CancelAsync(id, req.Reason);
        return s == null ? NotFound() : Ok(s);
    }
}