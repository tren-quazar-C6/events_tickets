using events_tickets.Models;
using events_tickets.Services;
using Microsoft.AspNetCore.Mvc;

namespace events_tickets.Controllers;

public class DashboardController : Controller
{
    private readonly ApiService _api;
    private readonly SessionService _session;

    public DashboardController(ApiService api, SessionService session)
    {
        _api = api;
        _session = session;
    }

    public async Task<IActionResult> Index()
    {
        if (!_session.IsAuthenticated())
            return RedirectToAction("Login", "Auth");

        var result = await _api.GetEventosAsync();
        if (!result.Success)
        {
            TempData["message"] = result.Message;
            TempData["success"] = "False";
            return View(new List<EventoResumen>());
        }

        ViewBag.Employee = _session.GetEmployee();
        return View(result.Data);
    }
}