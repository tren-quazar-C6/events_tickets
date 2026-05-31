using events_tickets.Models;
using events_tickets.Services;
using Microsoft.AspNetCore.Mvc;

namespace events_tickets.Controllers;

public class DashboardController : Controller
{
    private readonly IEventService _events;
    private readonly SessionService _session;

    public DashboardController(IEventService events, SessionService session)
    {
        _events = events;
        _session = session;
    }

    public async Task<IActionResult> Index()
    {
        if (!_session.IsAuthenticated())
            return RedirectToAction("Login", "Auth");

        ViewBag.Employee = _session.GetEmployee();
        return View(await _events.GetActiveAsync());
    }
}
