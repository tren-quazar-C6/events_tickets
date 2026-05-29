using Dapper;
using events_tickets.Dtos;
using events_tickets.Infrastructure;
using events_tickets.Models;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace events_tickets.Services;

public class TicketService : ITicketService
{
    private readonly IDbConnectionFactory _db;

    public TicketService(IDbConnectionFactory db) => _db = db;

    public async Task<List<Ticket>> GenerarAsync(Venta venta, List<AsientoInfo> asientos)
    {
        var tickets = asientos.Select(a =>
        {
            var qrToken = Guid.NewGuid().ToString("N");
            var codigo = $"TKT-{venta.IdEvento}-{a.IdEventoAsiento}-{qrToken[..8].ToUpper()}";
            return new Ticket
            {
                IdVenta = venta.IdVenta,
                IdEvento = venta.IdEvento,
                IdCliente = venta.IdCliente,
                IdEventoAsiento = a.IdEventoAsiento,
                CodigoAsiento = a.CodigoAsiento,
                Zona = a.Zona,
                CodigoUnico = codigo,
                QrToken = qrToken,
                QrImagenBase64 = GenerarQr(qrToken),
                PrecioPagado = a.Precio
            };
        }).ToList();

        using var conn = _db.Create();
        foreach (var t in tickets)
        {
            await conn.ExecuteAsync("""
                                    INSERT INTO tickets
                                      (id_venta, id_evento, id_cliente, id_evento_asiento, codigo_asiento,
                                       zona, codigo_unico, qr_token, qr_imagen_base64, precio_pagado)
                                    VALUES
                                      (@IdVenta, @IdEvento, @IdCliente, @IdEventoAsiento, @CodigoAsiento,
                                       @Zona, @CodigoUnico, @QrToken, @QrImagenBase64, @PrecioPagado)
                                    """, t);
            t.IdTicket = await conn.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID()");
        }

        return tickets;
    }
    public async Task<TicketDetalleDto?> ObtenerAsync(int id)
    {
        using var conn = _db.Create();
        var sql = """
            SELECT t.*, c.nombre as nombre_cliente
            FROM tickets t
            JOIN clientes c ON c.id_cliente = t.id_cliente
            WHERE t.id_ticket = @id
            """;
        var t = await conn.QueryFirstOrDefaultAsync(sql, new { id });
        if (t == null) return null;
        return new TicketDetalleDto
        {
            IdTicket = t.id_ticket,
            CodigoUnico = t.codigo_unico,
            QrToken = t.qr_token,
            CodigoAsiento = t.codigo_asiento,
            Zona = t.zona,
            PrecioPagado = t.precio_pagado,
            EstadoTicket = t.estado_ticket,
            FechaEmision = t.fecha_emision,
            IdEvento = t.id_evento,
            IdCliente = t.id_cliente,
            NombreCliente = t.nombre_cliente
        };
    }

    public async Task<List<TicketResumenDto>> ObtenerPorVentaAsync(int idVenta)
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync(
            "SELECT * FROM tickets WHERE id_venta = @idVenta", new { idVenta });
        return rows.Select(ToResumen).ToList();
    }

    public async Task<List<TicketResumenDto>> ObtenerPorClienteAsync(int idCliente)
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync(
            "SELECT * FROM tickets WHERE id_cliente = @idCliente ORDER BY fecha_emision DESC",
            new { idCliente });
        return rows.Select(ToResumen).ToList();
    }

    public async Task<byte[]> GenerarPdfAsync(int idTicket)
    {
        var ticket = await ObtenerAsync(idTicket)
            ?? throw new InvalidOperationException("Ticket no encontrado");

        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A6.Landscape());
            page.Margin(20);
            page.Content().Column(col =>
            {
                col.Item().Text($"Evento #{ticket.IdEvento}").Bold().FontSize(16);
                col.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item().Text($"Cliente: {ticket.NombreCliente}").FontSize(11);
                        inner.Item().Text($"Zona: {ticket.Zona}").FontSize(11);
                        inner.Item().Text($"Asiento: {ticket.CodigoAsiento}").FontSize(14).Bold();
                        inner.Item().PaddingTop(4).Text($"Código: {ticket.CodigoUnico}").FontSize(9);
                        inner.Item().Text($"Estado: {ticket.EstadoTicket}").FontSize(8).Italic();
                        inner.Item().Text($"Precio: ${ticket.PrecioPagado:F2}").FontSize(10);
                    });
                    if (!string.IsNullOrEmpty(ticket.QrToken))
                    {
                        var qrBytes = Convert.FromBase64String(GenerarQr(ticket.QrToken));
                        row.ConstantItem(85).Image(qrBytes);
                    }
                });
            });
        })).GeneratePdf();
    }

    private static string GenerarQr(string contenido)
    {
        using var gen = new QRCodeGenerator();
        var data = gen.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.M);
        using var png = new PngByteQRCode(data);
        return Convert.ToBase64String(png.GetGraphic(10));
    }

    private static TicketResumenDto ToResumen(dynamic r) => new()
    {
        IdTicket = r.id_ticket,
        CodigoUnico = r.codigo_unico,
        QrToken = r.qr_token,
        CodigoAsiento = r.codigo_asiento,
        Zona = r.zona,
        PrecioPagado = r.precio_pagado,
        EstadoTicket = r.estado_ticket,
        FechaEmision = r.fecha_emision
    };
}