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
                e.id_evento,
                e.nombre_evento,
                e.descripcion,
                e.fecha_evento,
                e.fecha_inicio_ventas,
                e.fecha_fin_ventas,
                e.capacidad_total,
                te.nombre_tipo AS tipo_evento,
                NULL AS imagen_principal,
                SUM(CASE WHEN ea.estado = 'DISPONIBLE' THEN 1 ELSE 0 END) AS asientos_disponibles,
                MIN(ez.precio) AS precio_desde
            FROM EVENTOS e
            LEFT JOIN TIPO_EVENTO te ON te.id_tipo_evento = e.id_tipo_evento
            LEFT JOIN EVENTO_ZONA ez ON ez.id_evento = e.id_evento
            LEFT JOIN EVENTO_ASIENTO ea ON ea.id_evento = e.id_evento
            WHERE COALESCE(e.activo, 1) = 1
              AND COALESCE(e.publicado, 1) = 1
            GROUP BY e.id_evento
            ORDER BY e.fecha_evento
            """);

        return rows.ToList();
    }

    public async Task<EventoDetalle?> GetAsync(int id)
    {
        using var conn = _db.Create();
        var evento = await conn.QueryFirstOrDefaultAsync<EventoDetalle>("""
            SELECT
                e.id_evento,
                e.nombre_evento,
                e.descripcion,
                e.fecha_evento,
                e.fecha_inicio_ventas,
                e.fecha_fin_ventas,
                e.capacidad_total,
                te.nombre_tipo AS tipo_evento,
                NULL AS imagen_principal,
                SUM(CASE WHEN ea.estado = 'DISPONIBLE' THEN 1 ELSE 0 END) AS asientos_disponibles,
                SUM(CASE WHEN ea.estado = 'RESERVADO' THEN 1 ELSE 0 END) AS asientos_reservados,
                SUM(CASE WHEN ea.estado = 'VENDIDO' THEN 1 ELSE 0 END) AS asientos_vendidos,
                MIN(ez.precio) AS precio_desde
            FROM EVENTOS e
            LEFT JOIN TIPO_EVENTO te ON te.id_tipo_evento = e.id_tipo_evento
            LEFT JOIN EVENTO_ZONA ez ON ez.id_evento = e.id_evento
            LEFT JOIN EVENTO_ASIENTO ea ON ea.id_evento = e.id_evento
            WHERE e.id_evento = @id
            GROUP BY e.id_evento
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
                ea.id_evento_asiento,
                a.id_asiento AS id_asiento,
                CONCAT(a.fila, '-', a.numero) AS codigo_asiento,
                a.fila,
                a.numero,
                z.id_zona AS id_zona,
                z.nombre_zona AS zona,
                z.color_hex AS color_zona,
                ez.precio,
                ea.estado
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
}
