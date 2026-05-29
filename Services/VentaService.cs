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
        using var conn = _db.Create();
        conn.Open();
        using var tx = conn.BeginTransaction();

        // Lock and validate seat availability atomically
        var asientos = (await conn.QueryAsync("""
            SELECT ea.id_evento_asiento, a.codigo_asiento, z.nombre_zona AS zona, ez.precio
            FROM evento_asientos ea
            JOIN asientos a ON a.id_asiento = ea.id_asiento
            JOIN evento_zonas ez ON ez.id_evento_zona = ea.id_evento_zona
            JOIN zonas z ON z.id_zona = ez.id_zona
            WHERE ea.id_evento_asiento IN @ids
              AND ea.estado = 'disponible'
            FOR UPDATE
            """, new { ids = req.IdEventoAsientos }, tx)).ToList();

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
            INSERT INTO ventas (id_evento, id_cliente, id_staff, subtotal, total, notas)
            VALUES (@IdEvento, @IdCliente, @IdStaff, @Subtotal, @Total, @Notas)
            """, venta, tx);
        venta.IdVenta = await conn.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID()", transaction: tx);

        foreach (var a in asientos)
            await conn.ExecuteAsync(
                "INSERT INTO venta_asientos (id_venta, id_evento_asiento) VALUES (@v, @a)",
                new { v = venta.IdVenta, a = (int)a.id_evento_asiento }, tx);

        await conn.ExecuteAsync(
            "UPDATE evento_asientos SET estado = 'vendido' WHERE id_evento_asiento IN @ids",
            new { ids = req.IdEventoAsientos }, tx);

        tx.Commit();

        var asientoInfos = asientos.Select(a => new AsientoInfo(
            (int)a.id_evento_asiento,
            (string)a.codigo_asiento,
            (string)a.zona,
            (decimal)a.precio
        )).ToList();

        var tickets = await _tickets.GenerarAsync(venta, asientoInfos);

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
        var v = await conn.QueryFirstOrDefaultAsync<Venta>(
            "SELECT * FROM ventas WHERE id_venta = @id", new { id });
        if (v == null) return null;
        var tickets = await _tickets.ObtenerPorVentaAsync(id);
        return new VentaDetalleDto
        {
            IdVenta = v.IdVenta, IdEvento = v.IdEvento, IdCliente = v.IdCliente,
            IdStaff = v.IdStaff, Total = v.Total, Estado = v.Estado,
            FechaVenta = v.FechaVenta, CantidadTickets = tickets.Count, Tickets = tickets
        };
    }

    public async Task<List<VentaResumenDto>> ObtenerPorClienteAsync(int idCliente)
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<Venta>(
            "SELECT * FROM ventas WHERE id_cliente = @idCliente ORDER BY fecha_venta DESC",
            new { idCliente });
        return rows.Select(v => new VentaResumenDto
        {
            IdVenta = v.IdVenta, IdEvento = v.IdEvento, IdCliente = v.IdCliente,
            IdStaff = v.IdStaff, Total = v.Total, Estado = v.Estado, FechaVenta = v.FechaVenta
        }).ToList();
    }

    public async Task<VentaResumenDto?> CancelarAsync(int id, string motivo)
    {
        using var conn = _db.Create();
        conn.Open();
        using var tx = conn.BeginTransaction();

        var venta = await conn.QueryFirstOrDefaultAsync<Venta>(
            "SELECT * FROM ventas WHERE id_venta = @id FOR UPDATE", new { id }, tx);
        if (venta == null) return null;

        await conn.ExecuteAsync(
            "UPDATE ventas SET estado = 'cancelada', fecha_cancelacion = NOW() WHERE id_venta = @id",
            new { id }, tx);

        await conn.ExecuteAsync("""
            UPDATE evento_asientos ea
            JOIN venta_asientos va ON va.id_evento_asiento = ea.id_evento_asiento
            SET ea.estado = 'disponible'
            WHERE va.id_venta = @id
            """, new { id }, tx);

        await conn.ExecuteAsync(
            "UPDATE tickets SET estado_ticket = 'cancelado' WHERE id_venta = @id",
            new { id }, tx);

        tx.Commit();

        venta.Estado = "cancelada";
        return new VentaResumenDto
        {
            IdVenta = venta.IdVenta, IdEvento = venta.IdEvento, IdCliente = venta.IdCliente,
            IdStaff = venta.IdStaff, Total = venta.Total, Estado = venta.Estado,
            FechaVenta = venta.FechaVenta
        };
    }
}