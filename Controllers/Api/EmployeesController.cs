using events_tickets.Contracts;
using events_tickets.Services;
using Microsoft.AspNetCore.Mvc;

namespace events_tickets.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _svc;
    public EmployeesController(IEmployeeService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetActive() => Ok(await _svc.GetActiveAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest req) =>
        Ok(await _svc.CreateAsync(req));
}