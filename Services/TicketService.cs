using Dapper;
using System.Data;
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
        conn.Open();
        return await InsertarAsync(tickets, conn, null);
    }

    public async Task<List<Ticket>> GenerarAsync(Venta venta, List<AsientoInfo> asientos, IDbConnection conn, IDbTransaction tx)
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

        return await InsertarAsync(tickets, conn, tx);
    }

    private static async Task<List<Ticket>> InsertarAsync(List<Ticket> tickets, IDbConnection conn, IDbTransaction? tx)
    {
        foreach (var t in tickets)
        {
            await conn.ExecuteAsync("""
                                    INSERT INTO TICKETS
                                      (id_venta, id_estado_ticket, id_evento_asiento, codigo_unico,
                                       qr_token, precio_pagado, fecha_generacion, fecha_impresion)
                                    VALUES
                                      (@IdVenta, 2, @IdEventoAsiento, @CodigoUnico,
                                       @QrToken, @PrecioPagado, UTC_TIMESTAMP(), UTC_TIMESTAMP())
                                    """, t, tx);
            t.IdTicket = await conn.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID()", transaction: tx);
            t.EstadoTicket = "PAGADO";
            t.FechaEmision = DateTime.UtcNow;
        }

        return tickets;
    }
    public async Task<TicketDetalleDto?> ObtenerAsync(int id)
    {
        using var conn = _db.Create();
        var sql = """
            SELECT
                t.id_ticket,
                t.codigo_unico,
                t.qr_token,
                t.precio_pagado,
                t.fecha_generacion,
                et.nombre_estado AS estado_ticket,
                u.id_usuario AS id_cliente,
                u.nombre AS nombre_cliente,
                e.id_evento,
                e.nombre_evento,
                e.fecha_evento,
                z.nombre_zona AS zona,
                CONCAT(a.fila, '-', a.numero) AS codigo_asiento
            FROM TICKETS t
            JOIN ESTADO_TICKET et ON et.id_estado_ticket = t.id_estado_ticket
            JOIN VENTAS v ON v.id_venta = t.id_venta
            JOIN USUARIO u ON u.id_usuario = v.id_usuario
            JOIN EVENTO_ASIENTO ea ON ea.id_evento_asiento = t.id_evento_asiento
            JOIN EVENTOS e ON e.id_evento = ea.id_evento
            JOIN ASIENTOS a ON a.id_asiento = ea.id_asiento
            JOIN ZONAS z ON z.id_zona = a.id_zona
            WHERE t.id_ticket = @id
            """;
        var t = await conn.QueryFirstOrDefaultAsync(sql, new { id });
        return t == null ? null : ToDetalle(t);
    }

    public async Task<TicketDetalleDto?> ObtenerPorCodigoAsync(string codigoOQrToken)
    {
        using var conn = _db.Create();
        var sql = """
            SELECT
                t.id_ticket,
                t.codigo_unico,
                t.qr_token,
                t.precio_pagado,
                t.fecha_generacion,
                et.nombre_estado AS estado_ticket,
                u.id_usuario AS id_cliente,
                u.nombre AS nombre_cliente,
                e.id_evento,
                e.nombre_evento,
                e.fecha_evento,
                z.nombre_zona AS zona,
                CONCAT(a.fila, '-', a.numero) AS codigo_asiento
            FROM TICKETS t
            JOIN ESTADO_TICKET et ON et.id_estado_ticket = t.id_estado_ticket
            JOIN VENTAS v ON v.id_venta = t.id_venta
            JOIN USUARIO u ON u.id_usuario = v.id_usuario
            JOIN EVENTO_ASIENTO ea ON ea.id_evento_asiento = t.id_evento_asiento
            JOIN EVENTOS e ON e.id_evento = ea.id_evento
            JOIN ASIENTOS a ON a.id_asiento = ea.id_asiento
            JOIN ZONAS z ON z.id_zona = a.id_zona
            WHERE t.codigo_unico = @codigoOQrToken OR t.qr_token = @codigoOQrToken
            """;
        var t = await conn.QueryFirstOrDefaultAsync(sql, new { codigoOQrToken });
        return t == null ? null : ToDetalle(t);
    }

    public async Task<List<TicketResumenDto>> ObtenerPorVentaAsync(int idVenta)
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync("""
            SELECT t.id_ticket, t.codigo_unico, t.qr_token, t.precio_pagado, t.fecha_generacion,
                   et.nombre_estado AS estado_ticket,
                   z.nombre_zona AS zona, CONCAT(a.fila, '-', a.numero) AS codigo_asiento
            FROM TICKETS t
            JOIN ESTADO_TICKET et ON et.id_estado_ticket = t.id_estado_ticket
            JOIN EVENTO_ASIENTO ea ON ea.id_evento_asiento = t.id_evento_asiento
            JOIN ASIENTOS a ON a.id_asiento = ea.id_asiento
            JOIN ZONAS z ON z.id_zona = a.id_zona
            WHERE t.id_venta = @idVenta
            ORDER BY t.id_ticket
            """, new { idVenta });
        return rows.Select(ToResumen).ToList();
    }

    public async Task<List<TicketResumenDto>> ObtenerPorClienteAsync(int idCliente)
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync("""
            SELECT t.id_ticket, t.codigo_unico, t.qr_token, t.precio_pagado, t.fecha_generacion,
                   et.nombre_estado AS estado_ticket,
                   z.nombre_zona AS zona, CONCAT(a.fila, '-', a.numero) AS codigo_asiento
            FROM TICKETS t
            JOIN ESTADO_TICKET et ON et.id_estado_ticket = t.id_estado_ticket
            JOIN VENTAS v ON v.id_venta = t.id_venta
            JOIN EVENTO_ASIENTO ea ON ea.id_evento_asiento = t.id_evento_asiento
            JOIN ASIENTOS a ON a.id_asiento = ea.id_asiento
            JOIN ZONAS z ON z.id_zona = a.id_zona
            WHERE v.id_usuario = @idCliente
            ORDER BY t.fecha_generacion DESC
            """, new { idCliente });
        return rows.Select(ToResumen).ToList();
    }

    public async Task<TicketDetalleDto?> ValidarAsync(string codigoOQrToken, int idStaff)
    {
        using var conn = _db.Create();
        conn.Open();
        using var tx = conn.BeginTransaction();

        var ticket = await conn.QueryFirstOrDefaultAsync("""
            SELECT * FROM TICKETS
            WHERE (codigo_unico = @codigoOQrToken OR qr_token = @codigoOQrToken)
              AND id_estado_ticket = 2
            FOR UPDATE
            """, new { codigoOQrToken }, tx);

        if (ticket == null)
        {
            tx.Rollback();
            return null;
        }

        await conn.ExecuteAsync("""
            UPDATE TICKETS
            SET id_estado_ticket = 3
            WHERE id_ticket = @idTicket
            """, new { idTicket = (int)ticket.id_ticket }, tx);

        tx.Commit();
        return await ObtenerAsync((int)ticket.id_ticket);
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
        FechaEmision = r.fecha_generacion
    };

    private static TicketDetalleDto ToDetalle(dynamic t) => new()
    {
        IdTicket = t.id_ticket,
        CodigoUnico = t.codigo_unico,
        QrToken = t.qr_token,
        CodigoAsiento = t.codigo_asiento,
        Zona = t.zona,
        PrecioPagado = t.precio_pagado,
        EstadoTicket = t.estado_ticket,
        FechaEmision = t.fecha_generacion,
        IdEvento = t.id_evento,
        NombreEvento = t.nombre_evento,
        FechaEvento = t.fecha_evento,
        IdCliente = t.id_cliente,
        NombreCliente = t.nombre_cliente
    };
}
