using Dapper;
using events_tickets.Contracts;
using events_tickets.Dtos;
using events_tickets.Infrastructure;
using events_tickets.Models;

namespace events_tickets.Services;

public class VentaService : IVentaService
{
    private readonly IDbConnectionFactory _db;
    private readonly ITicketService _tickets;

    public VentaService(IDbConnectionFactory db, ITicketService tickets)
    {
        _db = db;
        _tickets = tickets;
    }

    public async Task<VentaDetalleDto> CrearAsync(CrearVentaRequest req)
    {
        if (req.IdEventoAsientos.Count == 0)
            throw new InvalidOperationException("Debe seleccionar al menos un asiento");

        if (req.IdEventoAsientos.Count != req.IdEventoAsientos.Distinct().Count())
            throw new InvalidOperationException("La venta contiene asientos duplicados");

        using var conn = _db.Create();
        conn.Open();
        using var tx = conn.BeginTransaction();

        // Lock and validate seat availability atomically
        var asientos = (await conn.QueryAsync("""
            SELECT ea.id_evento_asiento, CONCAT(a.fila, '-', a.numero) AS codigo_asiento, z.nombre_zona AS zona, ez.precio
            FROM EVENTO_ASIENTO ea
            JOIN ASIENTOS a ON a.id_asiento = ea.id_asiento
            JOIN EVENTO_ZONA ez ON ez.id_evento = ea.id_evento AND ez.id_zona = a.id_zona
            JOIN ZONAS z ON z.id_zona = a.id_zona
            WHERE ea.id_evento_asiento IN @ids
              AND ea.id_evento = @idEvento
              AND ea.estado = 'DISPONIBLE'
            FOR UPDATE
            """, new { ids = req.IdEventoAsientos, idEvento = req.IdEvento }, tx)).ToList();

        if (asientos.Count != req.IdEventoAsientos.Count)
            throw new InvalidOperationException("Uno o más asientos no están disponibles");

        var total = asientos.Sum(a => (decimal)a.precio);

        var venta = new Venta
        {
            IdEvento = req.IdEvento,
            IdCliente = req.IdCliente,
            IdStaff = req.IdStaff,
            Subtotal = total,
            Total = total,
            Notas = req.Notas
        };

        await conn.ExecuteAsync("""
            INSERT INTO VENTAS (id_usuario, id_staff, tipo_venta, total, estado_pago, metodo_pago, referencia_interna, fecha_pago, fecha_venta)
            VALUES (@IdCliente, @IdStaff, 'TAQUILLA', @Total, 'APPROVED', 'TAQUILLA', @Referencia, UTC_TIMESTAMP(), UTC_TIMESTAMP())
            """, new
        {
            venta.IdCliente,
            venta.IdStaff,
            venta.Total,
            Referencia = $"TAQ-{Guid.NewGuid():N}"
        }, tx);
        venta.IdVenta = await conn.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID()", transaction: tx);

        await conn.ExecuteAsync(
            "UPDATE EVENTO_ASIENTO SET estado = 'VENDIDO' WHERE id_evento_asiento IN @ids",
            new { ids = req.IdEventoAsientos }, tx);

        var asientoInfos = asientos.Select(a => new AsientoInfo(
            (int)a.id_evento_asiento,
            (string)a.codigo_asiento,
            (string)a.zona,
            (decimal)a.precio
        )).ToList();

        var tickets = await _tickets.GenerarAsync(venta, asientoInfos, conn, tx);

        tx.Commit();

        return new VentaDetalleDto
        {
            IdVenta = venta.IdVenta,
            IdEvento = venta.IdEvento,
            IdCliente = venta.IdCliente,
            IdStaff = venta.IdStaff,
            Total = venta.Total,
            Estado = venta.Estado,
            FechaVenta = venta.FechaVenta,
            CantidadTickets = tickets.Count,
            Tickets = tickets.Select(t => new TicketResumenDto
            {
                IdTicket = t.IdTicket,
                CodigoUnico = t.CodigoUnico,
                QrToken = t.QrToken,
                CodigoAsiento = t.CodigoAsiento,
                Zona = t.Zona,
                PrecioPagado = t.PrecioPagado,
                EstadoTicket = t.EstadoTicket,
                FechaEmision = t.FechaEmision
            }).ToList()
        };
    }

    public async Task<VentaDetalleDto?> ObtenerAsync(int id)
    {
        using var conn = _db.Create();
        var v = await conn.QueryFirstOrDefaultAsync("""
            SELECT
                v.id_venta,
                ev.id_evento,
                e.nombre_evento,
                e.fecha_evento,
                u.id_usuario,
                u.nombre AS nombre_cliente,
                u.email AS email_cliente,
                u.telefono AS numero_documento_cliente,
                v.id_staff,
                v.total,
                v.estado_pago,
                v.fecha_venta
            FROM VENTAS v
            JOIN USUARIO u ON u.id_usuario = v.id_usuario
            LEFT JOIN TICKETS t ON t.id_venta = v.id_venta
            LEFT JOIN EVENTO_ASIENTO ev ON ev.id_evento_asiento = t.id_evento_asiento
            LEFT JOIN EVENTOS e ON e.id_evento = ev.id_evento
            WHERE v.id_venta = @id
            GROUP BY v.id_venta, ev.id_evento, e.nombre_evento, e.fecha_evento, u.id_usuario, u.nombre, u.email, u.telefono, v.id_staff, v.total, v.estado_pago, v.fecha_venta
            """, new { id });
        if (v == null) return null;
        var tickets = await _tickets.ObtenerPorVentaAsync(id);
        return new VentaDetalleDto
        {
            IdVenta = v.id_venta,
            IdEvento = v.id_evento ?? 0,
            NombreEvento = v.nombre_evento,
            FechaEvento = v.fecha_evento,
            IdCliente = v.id_usuario,
            NombreCliente = v.nombre_cliente,
            EmailCliente = v.email_cliente,
            NumeroDocumentoCliente = v.numero_documento_cliente,
            IdStaff = v.id_staff,
            Total = v.total,
            Estado = v.estado_pago,
            FechaVenta = v.fecha_venta,
            CantidadTickets = tickets.Count,
            Tickets = tickets
        };
    }

    public async Task<List<VentaResumenDto>> ObtenerPorClienteAsync(int idCliente)
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync("""
            SELECT v.*, COUNT(t.id_ticket) AS cantidad_tickets
            FROM VENTAS v
            LEFT JOIN TICKETS t ON t.id_venta = v.id_venta
            WHERE v.id_usuario = @idCliente
            GROUP BY v.id_venta
            ORDER BY v.fecha_venta DESC
            """,
            new { idCliente });
        return rows.Select(v => new VentaResumenDto
        {
            IdVenta = v.id_venta,
            IdEvento = 0,
            IdCliente = v.id_usuario,
            IdStaff = v.id_staff,
            Total = v.total,
            Estado = v.estado_pago,
            FechaVenta = v.fecha_venta,
            CantidadTickets = (int)v.cantidad_tickets
        }).ToList();
    }

    public async Task<VentaResumenDto?> CancelarAsync(int id, string motivo)
    {
        using var conn = _db.Create();
        conn.Open();
        using var tx = conn.BeginTransaction();

        var venta = await conn.QueryFirstOrDefaultAsync("""
            SELECT * FROM VENTAS WHERE id_venta = @id FOR UPDATE
            """, new { id }, tx);
        if (venta == null) return null;

        await conn.ExecuteAsync(
            "UPDATE VENTAS SET estado_pago = 'VOIDED' WHERE id_venta = @id",
            new { id }, tx);

        await conn.ExecuteAsync("""
            UPDATE EVENTO_ASIENTO ea
            JOIN TICKETS t ON t.id_evento_asiento = ea.id_evento_asiento
            SET ea.estado = 'DISPONIBLE'
            WHERE t.id_venta = @id
            """, new { id }, tx);

        await conn.ExecuteAsync(
            "UPDATE TICKETS SET id_estado_ticket = 4 WHERE id_venta = @id",
            new { id }, tx);

        tx.Commit();

        return new VentaResumenDto
        {
            IdVenta = venta.id_venta,
            IdEvento = 0,
            IdCliente = venta.id_usuario,
            IdStaff = venta.id_staff,
            Total = venta.total,
            Estado = "VOIDED",
            FechaVenta = venta.fecha_venta
        };
    }
}
