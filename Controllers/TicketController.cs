using events_tickets.Models;
using events_tickets.Services;
using Microsoft.AspNetCore.Mvc;

namespace events_tickets.Controllers;

public class TicketController : Controller
{
    private readonly IVentaService _ventas;
    private readonly SessionService _session;
    private readonly IPrintService _print;
    private readonly IEmailService _email;

    public TicketController(IVentaService ventas, SessionService session, IPrintService print, IEmailService email)
    {
        _ventas = ventas;
        _session = session;
        _print = print;
        _email = email;
    }

    public async Task<IActionResult> Print(int ventaId)
    {
        if (!_session.IsAuthenticated())
            return RedirectToAction("Login", "Auth");

        var venta = await _ventas.ObtenerAsync(ventaId);
        if (venta == null)
        {
            TempData["message"] = "Sale not found";
            TempData["success"] = "False";
            return RedirectToAction("Index", "Dashboard");
        }

        // Populate event info on each ticket for the view
        foreach (var ticket in venta.Tickets)
        {
            ticket.EventName = venta.NombreEvento;
            ticket.EventDate = venta.FechaEvento ?? venta.FechaVenta;

            await _print.ImprimirAsync(new Ticket
                {
                    IdTicket = ticket.IdTicket,
                    CodigoAsiento = ticket.CodigoAsiento,
                    Zona = ticket.Zona,
                    CodigoUnico = ticket.CodigoUnico,
                    QrToken = ticket.QrToken
                }, venta.NombreEvento ?? "", venta.FechaEvento ?? venta.FechaVenta,
                venta.NombreCliente ?? "", venta.NumeroDocumentoCliente ?? "");
        }

        try { ViewBag.EmailSent = await _email.SendTicketsAsync(venta); }
        catch { ViewBag.EmailSent = false; }

        return View(venta);
    }
}
