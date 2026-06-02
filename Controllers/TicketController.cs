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

        venta.AccessEmail = TempData["BuyerAccessEmail"] as string;
        venta.AccessPassword = TempData["BuyerAccessPassword"] as string;

        // Populate event info on each ticket for the view
        foreach (var ticket in venta.Tickets)
        {
            ticket.EventName = venta.NombreEvento;
            ticket.EventDate = venta.FechaEvento ?? venta.FechaVenta;
        }

        try { ViewBag.EmailSent = await _email.SendTicketsAsync(venta); }
        catch { ViewBag.EmailSent = false; }

        if (TempData["PrintStatus"] is string printStatus)
            ViewBag.PrintStatus = printStatus;

        return View(venta);
    }

    [HttpPost]
    public async Task<IActionResult> Reprint(int ventaId)
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

        var ok = true;
        foreach (var ticket in venta.Tickets)
        {
            var printed = await _print.ImprimirAsync(new Ticket
                {
                    IdTicket = ticket.IdTicket,
                    CodigoAsiento = ticket.CodigoAsiento,
                    Zona = ticket.Zona,
                    CodigoUnico = ticket.CodigoUnico,
                    QrToken = ticket.QrToken
                }, venta.NombreEvento ?? "", venta.FechaEvento ?? venta.FechaVenta,
                venta.NombreCliente ?? "", venta.NumeroDocumentoCliente ?? "");

            if (!printed) ok = false;
        }

        TempData["PrintStatus"] = ok
            ? "Reimpresión enviada a la impresora térmica."
            : "No se pudo completar la reimpresión en la impresora térmica.";

        return RedirectToAction(nameof(Print), new { ventaId });
    }
}
