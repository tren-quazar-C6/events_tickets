using Dapper;
using events_tickets.Infrastructure;
using events_tickets.Services;
using Microsoft.AspNetCore.Mvc;

namespace events_tickets.Controllers.Api;

[ApiController]
[Route("api/tickets")]
public class TicketsPrintController : ControllerBase
{
    private readonly ITicketService _tickets;
    private readonly IPrintService _print;
    private readonly IDbConnectionFactory _db;

    public TicketsPrintController(ITicketService tickets, IPrintService print, IDbConnectionFactory db)
    {
        _tickets = tickets;
        _print = print;
        _db = db;
    }

    [HttpGet("{id:int}/qr")]
    public async Task<IActionResult> Qr(int id)
    {
        var t = await _tickets.ObtenerAsync(id);
        if (t == null) return NotFound();
        // Regenerate QR from token
        using var gen = new QRCoder.QRCodeGenerator();
        var data = gen.CreateQrCode(t.QrToken, QRCoder.QRCodeGenerator.ECCLevel.M);
        using var png = new QRCoder.PngByteQRCode(data);
        return File(png.GetGraphic(10), "image/png");
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> Pdf(int id)
    {
        try
        {
            var bytes = await _tickets.GenerarPdfAsync(id);
            return File(bytes, "application/pdf", $"ticket-{id}.pdf");
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:int}/imprimir")]
    public async Task<IActionResult> Imprimir(int id)
    {
        var ticket = await _tickets.ObtenerAsync(id);
        if (ticket == null) return NotFound(ServiceResponse<object>.Fail("Ticket no encontrado"));

        // Get event + customer info for print
        using var conn = _db.Create();
        var info = await conn.QueryFirstOrDefaultAsync("""
            SELECT e.nombre_evento, e.fecha_evento, c.nombre, c.numero_documento
            FROM tickets t
            JOIN clientes c ON c.id_cliente = t.id_cliente
            LEFT JOIN eventos e ON e.id_evento = t.id_evento
            WHERE t.id_ticket = @id
            """, new { id });

        if (info == null) return BadRequest(ServiceResponse<object>.Fail("Datos incompletos"));

        var rawTicket = new events_tickets.Models.Ticket
        {
            IdTicket = ticket.IdTicket,
            Zona = ticket.Zona,
            CodigoAsiento = ticket.CodigoAsiento,
            CodigoUnico = ticket.CodigoUnico,
            QrImagenBase64 = ticket.QrToken // pass token, print server uses code
        };

        var ok = await _print.ImprimirAsync(
            rawTicket,
            (string)(info.nombre_evento ?? ""),
            (DateTime)(info.fecha_evento ?? DateTime.UtcNow),
            (string)(info.nombre ?? ""),
            (string)(info.numero_documento ?? "")
        );

        return ok
            ? Ok(ServiceResponse<object>.Ok(new { impreso = true }))
            : StatusCode(500, ServiceResponse<object>.Fail("Error al imprimir"));
    }
}