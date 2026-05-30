using events_tickets.Services;
using Microsoft.AspNetCore.Mvc;

namespace events_tickets.Controllers;

public class TicketController : Controller
{
    private readonly ApiService _api;
    private readonly SessionService _session;
    private readonly PrintService _print;

    public TicketController(ApiService api, SessionService session, PrintService print)
    {
        _api = api;
        _session = session;
        _print = print;
    }

    public async Task<IActionResult> Print(int ventaId)
    {
        if (!_session.IsAuthenticated())
            return RedirectToAction("Login", "Auth");

        var result = await _api.GetVentaAsync(ventaId);
        if (!result.Success)
        {
            TempData["message"] = result.Message;
            TempData["success"] = "False";
            return RedirectToAction("Index", "Dashboard");
        }

        foreach (var ticket in result.Data!.Tickets)
            await _print.PrintTicketAsync(ticket);

        return View(result.Data);
    }
}