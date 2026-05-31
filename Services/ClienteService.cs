using Dapper;
using events_tickets.Contracts;
using events_tickets.Dtos;
using events_tickets.Infrastructure;
using events_tickets.Models;

namespace events_tickets.Services;

public class ClienteService : IClienteService
{
    private readonly IDbConnectionFactory _db;

    public ClienteService(IDbConnectionFactory db) => _db = db;

    public async Task<ClienteDto> CrearAsync(CrearClienteRequest req)
    {
        using var conn = _db.Create();
        var existente = await conn.QueryFirstOrDefaultAsync<Cliente>("""
            SELECT
                id_usuario AS IdCliente,
                nombre AS Nombre,
                '' AS NumeroDocumento,
                email AS Email,
                telefono AS Telefono,
                fecha_registro AS FechaRegistro
            FROM USUARIO
            WHERE email = @Email
               OR (@Telefono IS NOT NULL AND telefono = @Telefono)
            LIMIT 1
            """, new { req.Email, req.Telefono });

        if (existente != null)
            return ToDto(existente);

        await conn.ExecuteAsync("""
            INSERT INTO USUARIO (nombre, email, password_hash, telefono, activo, fecha_registro)
            VALUES (@Nombre, @Email, NULL, @Telefono, 1, UTC_TIMESTAMP())
            """, new { req.Nombre, req.Email, req.Telefono });

        var id = await conn.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID()");
        var cliente = await conn.QuerySingleAsync<Cliente>("""
            SELECT
                id_usuario AS IdCliente,
                nombre AS Nombre,
                '' AS NumeroDocumento,
                email AS Email,
                telefono AS Telefono,
                fecha_registro AS FechaRegistro
            FROM USUARIO
            WHERE id_usuario = @id
            """, new { id });
        return ToDto(cliente);
    }

    public async Task<ClienteDto?> ObtenerAsync(int id)
    {
        using var conn = _db.Create();
        var c = await conn.QueryFirstOrDefaultAsync<Cliente>("""
            SELECT
                id_usuario AS IdCliente,
                nombre AS Nombre,
                '' AS NumeroDocumento,
                email AS Email,
                telefono AS Telefono,
                fecha_registro AS FechaRegistro
            FROM USUARIO
            WHERE id_usuario = @id
            """, new { id });
        return c == null ? null : ToDto(c);
    }

    public async Task<ClienteDto?> ObtenerPorDocumentoAsync(string doc)
    {
        using var conn = _db.Create();
        var c = await conn.QueryFirstOrDefaultAsync<Cliente>("""
            SELECT
                id_usuario AS IdCliente,
                nombre AS Nombre,
                '' AS NumeroDocumento,
                email AS Email,
                telefono AS Telefono,
                fecha_registro AS FechaRegistro
            FROM USUARIO
            WHERE email = @doc OR telefono = @doc
            LIMIT 1
            """, new { doc });
        return c == null ? null : ToDto(c);
    }

    public async Task<List<ClienteDto>> ListarAsync()
    {
        using var conn = _db.Create();
        var list = await conn.QueryAsync<Cliente>("""
            SELECT
                id_usuario AS IdCliente,
                nombre AS Nombre,
                '' AS NumeroDocumento,
                email AS Email,
                telefono AS Telefono,
                fecha_registro AS FechaRegistro
            FROM USUARIO
            ORDER BY nombre
            """);
        return list.Select(ToDto).ToList();
    }

    private static ClienteDto ToDto(Cliente c) => new()
    {
        IdCliente = c.IdCliente,
        Nombre = c.Nombre,
        NumeroDocumento = c.NumeroDocumento,
        Email = c.Email,
        Telefono = c.Telefono,
        FechaRegistro = c.FechaRegistro
    };
}
