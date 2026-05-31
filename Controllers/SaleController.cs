using events_tickets.Models;
using events_tickets.Contracts;
using events_tickets.Services;
using Microsoft.AspNetCore.Mvc;

namespace events_tickets.Controllers;

public class SaleController : Controller
{
    private readonly IClienteService _clientes;
    private readonly IEventService _events;
    private readonly IVentaService _ventas;
    private readonly SessionService _session;

    public SaleController(IClienteService clientes, IEventService events, IVentaService ventas, SessionService session)
    {
        _clientes = clientes;
        _events = events;
        _ventas = ventas;
        _session = session;
    }

    public async Task<IActionResult> Create(int eventoId)
    {
        if (!_session.IsAuthenticated())
            return RedirectToAction("Login", "Auth");

        var evento = await _events.GetAsync(eventoId);
        if (evento == null)
        {
            TempData["message"] = "Event not found";
            TempData["success"] = "False";
            return RedirectToAction("Index", "Dashboard");
        }

        var model = new SaleViewModel
        {
            EventoId = eventoId,
            Evento = evento,
            AsientosDisponibles = await _events.GetAvailableSeatsAsync(eventoId)
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

        try
        {
            var cliente = await _clientes.ObtenerPorDocumentoAsync(model.CustomerDocument)
                ?? await _clientes.CrearAsync(new CrearClienteRequest(
                    model.CustomerName,
                    model.CustomerDocument,
                    model.CustomerEmail,
                    model.CustomerPhone));

            var employee = _session.GetEmployee();
            if (employee == null)
                return RedirectToAction("Login", "Auth");

            var venta = await _ventas.CrearAsync(new CrearVentaRequest(
                model.EventoId,
                cliente.IdCliente,
                employee.IdStaff,
                model.AsientosSeleccionados));

            TempData["message"] = "Sale completed successfully";
            TempData["success"] = "True";
            return RedirectToAction("Print", "Ticket", new { ventaId = venta.IdVenta });
        }
        catch (InvalidOperationException ex)
        {
            TempData["message"] = ex.Message;
            TempData["success"] = "False";
            return await ReloadCreate(model);
        }
    }

    private async Task<IActionResult> ReloadCreate(SaleViewModel model)
    {
        model.Evento = await _events.GetAsync(model.EventoId);
        model.AsientosDisponibles = await _events.GetAvailableSeatsAsync(model.EventoId);
        return View("Create", model);
    }
}
