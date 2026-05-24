using Microsoft.AspNetCore.Mvc;

namespace events_tickets.Controllers.Api;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            service = "events_tickets",
            status = "ok",
            timestampUtc = DateTime.UtcNow
        });
    }
}
