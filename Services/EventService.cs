using Dapper;
using events_tickets.Contracts;
using events_tickets.Infrastructure;
using events_tickets.Models;

namespace events_tickets.Services;

public class EventService : IEventService
{
    private readonly IDbConnectionFactory _db;

    public EventService(IDbConnectionFactory db) => _db = db;

    public async Task<List<EventoResumen>> GetActiveAsync()
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<EventoResumen>("""
            SELECT
                e.id_evento AS IdEvento,
                e.nombre_evento AS NombreEvento,
                e.descripcion AS Descripcion,
                e.fecha_evento AS FechaEvento,
                e.fecha_inicio_ventas AS FechaInicioVentas,
                e.fecha_fin_ventas AS FechaFinVentas,
                e.capacidad_total AS CapacidadTotal,
                te.nombre_tipo AS TipoEvento,
                NULL AS ImagenPrincipal,
                (
                    SELECT COUNT(*)
                    FROM EVENTO_ASIENTO ea
                    WHERE ea.id_evento = e.id_evento
                      AND ea.estado = 'DISPONIBLE'
                ) AS AsientosDisponibles,
                (
                    SELECT MIN(ez.precio)
                    FROM EVENTO_ZONA ez
                    WHERE ez.id_evento = e.id_evento
                      AND COALESCE(ez.activo, 1) = 1
                ) AS PrecioDesde
            FROM EVENTOS e
            LEFT JOIN TIPO_EVENTO te ON te.id_tipo_evento = e.id_tipo_evento
            WHERE COALESCE(e.activo, 1) = 1
              AND COALESCE(e.publicado, 1) = 1
            ORDER BY e.fecha_evento
            """);

        return rows.ToList();
    }

    public async Task<EventoDetalle?> GetAsync(int id)
    {
        using var conn = _db.Create();
        var evento = await conn.QueryFirstOrDefaultAsync<EventoDetalle>("""
            SELECT
                e.id_evento AS IdEvento,
                e.nombre_evento AS NombreEvento,
                e.descripcion AS Descripcion,
                e.fecha_evento AS FechaEvento,
                e.fecha_inicio_ventas AS FechaInicioVentas,
                e.fecha_fin_ventas AS FechaFinVentas,
                e.capacidad_total AS CapacidadTotal,
                te.nombre_tipo AS TipoEvento,
                NULL AS ImagenPrincipal,
                (
                    SELECT COUNT(*)
                    FROM EVENTO_ASIENTO ea
                    WHERE ea.id_evento = e.id_evento
                      AND ea.estado = 'DISPONIBLE'
                ) AS AsientosDisponibles,
                (
                    SELECT COUNT(*)
                    FROM EVENTO_ASIENTO ea
                    WHERE ea.id_evento = e.id_evento
                      AND ea.estado = 'RESERVADO'
                ) AS AsientosReservados,
                (
                    SELECT COUNT(*)
                    FROM EVENTO_ASIENTO ea
                    WHERE ea.id_evento = e.id_evento
                      AND ea.estado = 'VENDIDO'
                ) AS AsientosVendidos,
                (
                    SELECT MIN(ez.precio)
                    FROM EVENTO_ZONA ez
                    WHERE ez.id_evento = e.id_evento
                      AND COALESCE(ez.activo, 1) = 1
                ) AS PrecioDesde
            FROM EVENTOS e
            LEFT JOIN TIPO_EVENTO te ON te.id_tipo_evento = e.id_tipo_evento
            WHERE e.id_evento = @id
            """, new { id });

        return evento;
    }

    public async Task<EventoDetalle> CreateAsync(CreateEventRequest req)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("""
            INSERT INTO EVENTOS
              (id_tipo_evento, creado_por_staff, nombre_evento, descripcion, fecha_evento, fecha_inicio_ventas, fecha_fin_ventas, capacidad_total, publicado, activo)
            VALUES
              (1, 13, @Name, @Description, @Date, NOW(), @Date, @TotalSeats, 1, 1)
            """, req);

        var id = await conn.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID()");
        return await GetAsync(id) ?? new EventoDetalle { IdEvento = id, NombreEvento = req.Name };
    }

    public Task<List<EventoAsiento>> CreateSeatsAsync(int eventId, List<SeatDefinition> seats) =>
        GetAvailableSeatsAsync(eventId);

    public async Task<List<EventoAsiento>> GetAvailableSeatsAsync(int eventId)
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<EventoAsiento>("""
            SELECT
                ea.id_evento_asiento AS IdEventoAsiento,
                a.id_asiento AS IdAsiento,
                CONCAT(a.fila, '-', a.numero) AS CodigoAsiento,
                a.fila AS Fila,
                a.numero AS Numero,
                z.id_zona AS IdZona,
                z.nombre_zona AS Zona,
                z.color_hex AS ColorZona,
                ez.precio AS Precio,
                ea.estado AS Estado
            FROM EVENTO_ASIENTO ea
            JOIN ASIENTOS a ON a.id_asiento = ea.id_asiento
            JOIN EVENTO_ZONA ez ON ez.id_evento = ea.id_evento AND ez.id_zona = a.id_zona
            JOIN ZONAS z ON z.id_zona = a.id_zona
            WHERE ea.id_evento = @eventId
              AND ea.estado = 'DISPONIBLE'
            ORDER BY z.nombre_zona, a.fila, a.numero
            """, new { eventId });

        return rows.ToList();
    }
    public async Task<List<EventoAsiento>> GetAllSeatsAsync(int eventId)
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<EventoAsiento>("""
            SELECT
                ea.id_evento_asiento AS IdEventoAsiento,
                a.id_asiento AS IdAsiento,
                CONCAT(a.fila, '-', a.numero) AS CodigoAsiento,
                a.fila AS Fila,
                a.numero AS Numero,
                z.id_zona AS IdZona,
                z.nombre_zona AS Zona,
                z.color_hex AS ColorZona,
                ez.precio AS Precio,
                ea.estado AS Estado
            FROM EVENTO_ASIENTO ea
            JOIN ASIENTOS a ON a.id_asiento = ea.id_asiento
            JOIN EVENTO_ZONA ez ON ez.id_evento = ea.id_evento AND ez.id_zona = a.id_zona
            JOIN ZONAS z ON z.id_zona = a.id_zona
            WHERE ea.id_evento = @eventId
            ORDER BY z.nombre_zona, a.fila, a.numero
            """, new { eventId });
        return rows.ToList();
    }
}
