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
        await conn.ExecuteAsync("""
            INSERT INTO clientes (nombre, numero_documento, email, telefono)
            VALUES (@Nombre, @NumeroDocumento, @Email, @Telefono)
            """, new { req.Nombre, req.NumeroDocumento, req.Email, req.Telefono });

        var id = await conn.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID()");
        var cliente = await conn.QuerySingleAsync<Cliente>(
            "SELECT * FROM clientes WHERE id_cliente = @id", new { id });
        return ToDto(cliente);
    }

    public async Task<ClienteDto?> ObtenerAsync(int id)
    {
        using var conn = _db.Create();
        var c = await conn.QueryFirstOrDefaultAsync<Cliente>(
            "SELECT * FROM clientes WHERE id_cliente = @id", new { id });
        return c == null ? null : ToDto(c);
    }

    public async Task<ClienteDto?> ObtenerPorDocumentoAsync(string doc)
    {
        using var conn = _db.Create();
        var c = await conn.QueryFirstOrDefaultAsync<Cliente>(
            "SELECT * FROM clientes WHERE numero_documento = @doc", new { doc });
        return c == null ? null : ToDto(c);
    }

    public async Task<List<ClienteDto>> ListarAsync()
    {
        using var conn = _db.Create();
        var list = await conn.QueryAsync<Cliente>("SELECT * FROM clientes ORDER BY nombre");
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