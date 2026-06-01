using Dapper;
using events_tickets.Contracts;
using events_tickets.Dtos;
using events_tickets.Infrastructure;
using events_tickets.Models;
using System.Security.Cryptography;
using System.Text;

namespace events_tickets.Services;

public class ClienteService : IClienteService
{
    private readonly IDbConnectionFactory _db;

    public ClienteService(IDbConnectionFactory db) => _db = db;

    public async Task SyncWithLaravelUsersAsync()
    {
        using var conn = _db.Create();
        conn.Open();

        var usuarios = await conn.QueryAsync<Cliente>("""
            SELECT
                id_usuario AS IdCliente,
                nombre AS Nombre,
                email AS Email,
                telefono AS Telefono,
                password_hash AS PasswordHash,
                fecha_registro AS FechaRegistro
            FROM USUARIO
            WHERE email IS NOT NULL AND email <> ''
            """);

        foreach (var usuario in usuarios)
        {
            var linked = await EnsureLinkedLaravelUserAsync(conn, usuario);
            if (!string.IsNullOrWhiteSpace(linked.PasswordTemporal))
            {
                await conn.ExecuteAsync("""
                    UPDATE USUARIO
                    SET password_hash = @PasswordHash
                    WHERE id_usuario = @IdUsuario
                    """, new
                {
                    PasswordHash = HashPassword(linked.PasswordTemporal),
                    IdUsuario = usuario.IdCliente
                });
            }
        }

        var laravelUsers = await conn.QueryAsync("""
            SELECT id, name, email, created_at
            FROM users
            WHERE email IS NOT NULL AND email <> ''
            """);

        foreach (var lu in laravelUsers)
        {
            var existsInUsuario = await conn.ExecuteScalarAsync<int>("""
                SELECT COUNT(1)
                FROM USUARIO
                WHERE email = @Email
                """, new { Email = (string)lu.email! });

            if (existsInUsuario > 0) continue;

            await conn.ExecuteAsync("""
                INSERT INTO USUARIO (nombre, email, password_hash, telefono, activo, fecha_registro)
                VALUES (@Nombre, @Email, NULL, NULL, 1, COALESCE(@FechaRegistro, UTC_TIMESTAMP()))
                """, new
            {
                Nombre = (string?)lu.name ?? "Cliente",
                Email = (string)lu.email!,
                FechaRegistro = (DateTime?)lu.created_at
            });
        }
    }

    public async Task<ClienteDto> CrearAsync(CrearClienteRequest req)
    {
        using var conn = _db.Create();
        conn.Open(); // keep the same session so LAST_INSERT_ID() works correctly

        var existente = await conn.QueryFirstOrDefaultAsync<Cliente>("""
                                                                     SELECT
                                                                         id_usuario AS IdCliente,
                                                                         nombre AS Nombre,
                                                                         '' AS NumeroDocumento,
                                                                         email AS Email,
                                                                         telefono AS Telefono,
                                                                         password_hash AS PasswordHash,
                                                                         fecha_registro AS FechaRegistro
                                                                     FROM USUARIO
                                                                     WHERE email = @Email
                                                                        OR (@Telefono IS NOT NULL AND telefono = @Telefono)
                                                                     LIMIT 1
                                                                     """, new { req.Email, req.Telefono });

        if (existente != null)
            return ToDto(existente);

        var tempPassword = GenerateTemporaryPassword();
        await conn.ExecuteAsync("""
                                INSERT INTO USUARIO (nombre, email, password_hash, telefono, activo, fecha_registro)
                                VALUES (@Nombre, @Email, @PasswordHash, @Telefono, 1, UTC_TIMESTAMP())
                                """, new
                                {
                                    req.Nombre,
                                    req.Email,
                                    req.Telefono,
                                    PasswordHash = HashPassword(tempPassword)
                                });

        var id = await conn.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID()");

        var cliente = await conn.QueryFirstOrDefaultAsync<Cliente>("""
                                                                   SELECT
                                                                       id_usuario AS IdCliente,
                                                                       nombre AS Nombre,
                                                                       '' AS NumeroDocumento,
                                                                       email AS Email,
                                                                       telefono AS Telefono,
                                                                       password_hash AS PasswordHash,
                                                                       fecha_registro AS FechaRegistro
                                                                   FROM USUARIO
                                                                   WHERE id_usuario = @id
                                                                   """, new { id })
                      ?? throw new InvalidOperationException("No se pudo recuperar el cliente recién creado.");

        var dto = ToDto(cliente);
        dto.CuentaCreada = true;
        dto.PasswordTemporal = tempPassword;
        dto.PasswordTemporal = (await EnsureLinkedLaravelUserAsync(conn, cliente, dto.PasswordTemporal)).PasswordTemporal;
        return dto;
    }

    public async Task<ClienteDto> ObtenerOCrearActualizarAsync(CrearClienteRequest req)
    {
        using var conn = _db.Create();
        conn.Open();

        var byEmail = await conn.QueryFirstOrDefaultAsync<Cliente>("""
            SELECT
                id_usuario AS IdCliente,
                nombre AS Nombre,
                '' AS NumeroDocumento,
                email AS Email,
                telefono AS Telefono,
                password_hash AS PasswordHash,
                fecha_registro AS FechaRegistro
            FROM USUARIO
            WHERE email = @Email
            LIMIT 1
            """, new { req.Email });

        // If email already exists, always use that same user and refresh temporary access password.
        if (byEmail != null)
        {
            var tempPassword = GenerateTemporaryPassword();
            await conn.ExecuteAsync("""
                UPDATE USUARIO
                SET nombre = @Nombre,
                    telefono = @Telefono
                    ,password_hash = @PasswordHash
                WHERE id_usuario = @Id
                """, new
            {
                req.Nombre,
                req.Telefono,
                PasswordHash = HashPassword(tempPassword),
                Id = byEmail.IdCliente
            });

            var updatedByEmail = await conn.QueryFirstOrDefaultAsync<Cliente>("""
                SELECT
                    id_usuario AS IdCliente,
                    nombre AS Nombre,
                    '' AS NumeroDocumento,
                    email AS Email,
                    telefono AS Telefono,
                    password_hash AS PasswordHash,
                    fecha_registro AS FechaRegistro
                FROM USUARIO
                WHERE id_usuario = @id
                """, new { id = byEmail.IdCliente })
                ?? throw new InvalidOperationException("No se pudo recuperar el cliente actualizado.");

            var dto = ToDto(updatedByEmail);
            dto.CuentaCreada = true;
            dto.PasswordTemporal = tempPassword;
            dto.PasswordTemporal = (await EnsureLinkedLaravelUserAsync(conn, updatedByEmail, dto.PasswordTemporal)).PasswordTemporal;
            return dto;
        }

        var byPhone = await conn.QueryFirstOrDefaultAsync<Cliente>("""
            SELECT
                id_usuario AS IdCliente,
                nombre AS Nombre,
                '' AS NumeroDocumento,
                email AS Email,
                telefono AS Telefono,
                password_hash AS PasswordHash,
                fecha_registro AS FechaRegistro
            FROM USUARIO
            WHERE @Telefono IS NOT NULL AND telefono = @Telefono
            LIMIT 1
            """, new { req.Telefono });

        if (byPhone == null)
            return await CrearAsync(req);

        var byPhoneTempPassword = GenerateTemporaryPassword();

        await conn.ExecuteAsync("""
            UPDATE USUARIO
            SET nombre = @Nombre,
                email = @Email,
                telefono = @Telefono,
                password_hash = COALESCE(@PasswordHash, password_hash)
            WHERE id_usuario = @Id
            """, new
        {
            req.Nombre,
            req.Email,
            req.Telefono,
            PasswordHash = byPhoneTempPassword != null ? HashPassword(byPhoneTempPassword) : null,
            Id = byPhone.IdCliente
        });

        var actualizado = await conn.QueryFirstOrDefaultAsync<Cliente>("""
            SELECT
                id_usuario AS IdCliente,
                nombre AS Nombre,
                '' AS NumeroDocumento,
                email AS Email,
                telefono AS Telefono,
                password_hash AS PasswordHash,
                fecha_registro AS FechaRegistro
            FROM USUARIO
            WHERE id_usuario = @id
            """, new { id = byPhone.IdCliente })
            ?? throw new InvalidOperationException("No se pudo recuperar el cliente actualizado.");

        var updatedDto = ToDto(actualizado);
        updatedDto.CuentaCreada = true;
        updatedDto.PasswordTemporal = byPhoneTempPassword;
        updatedDto.PasswordTemporal = (await EnsureLinkedLaravelUserAsync(conn, actualizado, updatedDto.PasswordTemporal)).PasswordTemporal;
        return updatedDto;
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
                password_hash AS PasswordHash,
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
                password_hash AS PasswordHash,
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
                password_hash AS PasswordHash,
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

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        var bytes = RandomNumberGenerator.GetBytes(10);
        var pwd = new char[10];
        for (var i = 0; i < pwd.Length; i++)
            pwd[i] = chars[bytes[i] % chars.Length];
        return new string(pwd);
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private async Task<(bool Created, string? PasswordTemporal)> EnsureLinkedLaravelUserAsync(
        System.Data.IDbConnection conn,
        Cliente cliente,
        string? knownPlainPassword = null)
    {
        if (string.IsNullOrWhiteSpace(cliente.Email))
            return (false, null);

        var exists = await conn.ExecuteScalarAsync<int>("""
            SELECT COUNT(1)
            FROM users
            WHERE email = @Email
            """, new { Email = cliente.Email });

        if (exists > 0)
        {
            if (!string.IsNullOrWhiteSpace(knownPlainPassword))
            {
                var bcryptKnown = BCrypt.Net.BCrypt.HashPassword(knownPlainPassword);
                await conn.ExecuteAsync("""
                    UPDATE users
                    SET name = @Name, password = @Password,
                        email_verified_at = COALESCE(email_verified_at, UTC_TIMESTAMP()),
                        updated_at = UTC_TIMESTAMP()
                    WHERE email = @Email
                    """, new { Name = cliente.Nombre, Password = bcryptKnown, Email = cliente.Email });
            }
            else
            {
                await conn.ExecuteAsync("""
                    UPDATE users
                    SET name = @Name,
                        email_verified_at = COALESCE(email_verified_at, UTC_TIMESTAMP()),
                        updated_at = UTC_TIMESTAMP()
                    WHERE email = @Email
                    """, new { Name = cliente.Nombre, Email = cliente.Email });
            }
            return (false, knownPlainPassword);
        }

        var plainPassword = knownPlainPassword ?? GenerateTemporaryPassword();
        var bcrypt = BCrypt.Net.BCrypt.HashPassword(plainPassword);

        await conn.ExecuteAsync("""
            INSERT INTO users (name, email, password, email_verified_at, created_at, updated_at)
            VALUES (@Name, @Email, @Password, UTC_TIMESTAMP(), UTC_TIMESTAMP(), UTC_TIMESTAMP())
            """, new
        {
            Name = cliente.Nombre,
            Email = cliente.Email,
            Password = bcrypt
        });

        return (true, plainPassword);
    }
}
