using events_tickets.Models;
using events_tickets.Services;
using Microsoft.AspNetCore.Mvc;

namespace events_tickets.Controllers;

public class SaleController : Controller
{
    private readonly ApiService _api;
    private readonly SessionService _session;

    public SaleController(ApiService api, SessionService session)
    {
        _api = api;
        _session = session;
    }

    public async Task<IActionResult> Create(int eventoId)
    {
        if (!_session.IsAuthenticated())
            return RedirectToAction("Login", "Auth");

        var eventoResult = await _api.GetEventoAsync(eventoId);
        if (!eventoResult.Success)
        {
            TempData["message"] = eventoResult.Message;
            TempData["success"] = "False";
            return RedirectToAction("Index", "Dashboard");
        }

        var asientosResult = await _api.GetAsientosAsync(eventoId);

        var model = new SaleViewModel
        {
            EventoId = eventoId,
            Evento = eventoResult.Data,
            AsientosDisponibles = asientosResult.Data ?? new()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Store(SaleViewModel model, string asientosJson)
    {
        if (!_session.IsAuthenticated())
            return RedirectToAction("Login", "Auth");

        // Parse selected seats from the hidden JSON field
        if (!string.IsNullOrEmpty(asientosJson))
        {
            model.AsientosSeleccionados = System.Text.Json.JsonSerializer
                .Deserialize<List<int>>(asientosJson) ?? new();
        }

        if (model.AsientosSeleccionados.Count == 0)
        {
            TempData["message"] = "Please select at least one seat";
            TempData["success"] = "False";
            return await ReloadCreate(model);
        }

        var result = await _api.CreateVentaAsync(model);
        if (!result.Success)
        {
            TempData["message"] = result.Message;
            TempData["success"] = "False";
            return await ReloadCreate(model);
        }

        TempData["message"] = "Sale completed successfully";
        TempData["success"] = "True";
        return RedirectToAction("Print", "Ticket", new { ventaId = result.Data!.Id });
    }

    private async Task<IActionResult> ReloadCreate(SaleViewModel model)
    {
        var eventoResult = await _api.GetEventoAsync(model.EventoId);
        var asientosResult = await _api.GetAsientosAsync(model.EventoId);
        model.Evento = eventoResult.Data;
        model.AsientosDisponibles = asientosResult.Data ?? new();
        return View("Create", model);
    }
}