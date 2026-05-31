using Dapper;
using events_tickets.Contracts;
using events_tickets.Infrastructure;
using events_tickets.Models;
using System.Security.Cryptography;
using System.Text;

namespace events_tickets.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IDbConnectionFactory _db;

    public EmployeeService(IDbConnectionFactory db) => _db = db;

    public async Task<Employee> CreateAsync(CreateEmployeeRequest req)
    {
        using var conn = _db.Create();
        var passwordHash = HashPassword("123456");
        await conn.ExecuteAsync("""
            INSERT INTO STAFF (id_rol_staff, nombre, email, password_hash, activo, fecha_registro)
            VALUES (2, @FullName, '', @PasswordHash, 1, UTC_TIMESTAMP())
            """, new { req.FullName, PasswordHash = passwordHash });

        var id = await conn.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID()");
        return await GetAsync(id.ToString()) ?? new Employee { IdStaff = id, Nombre = req.FullName, Rol = req.Position };
    }

    public async Task<Employee?> GetAsync(string id)
    {
        using var conn = _db.Create();
        return await conn.QueryFirstOrDefaultAsync<Employee>("""
            SELECT s.id_staff, s.nombre, s.email, r.nombre_rol AS Rol
            FROM STAFF s
            JOIN ROL_STAFF r ON r.id_rol_staff = s.id_rol_staff
            WHERE s.id_staff = @id
            """, new { id });
    }

    public async Task<List<Employee>> GetActiveAsync()
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<Employee>("""
            SELECT s.id_staff, s.nombre, s.email, r.nombre_rol AS Rol
            FROM STAFF s
            JOIN ROL_STAFF r ON r.id_rol_staff = s.id_rol_staff
            WHERE COALESCE(s.activo, 1) = 1
              AND COALESCE(r.activo, 1) = 1
            ORDER BY s.nombre
            """);

        return rows.ToList();
    }

    public async Task<Employee?> LoginAsync(string email, string password)
    {
        using var conn = _db.Create();
        var hash = HashPassword(password);
        var row = await conn.QueryFirstOrDefaultAsync("""
            SELECT s.id_staff, s.nombre, s.email, s.password_hash, s.activo, r.nombre_rol
            FROM STAFF s
            JOIN ROL_STAFF r ON r.id_rol_staff = s.id_rol_staff
            WHERE email = @email
              AND COALESCE(s.activo, 1) = 1
              AND COALESCE(r.activo, 1) = 1
              AND UPPER(r.nombre_rol) IN ('TAQUILLA', 'ADMIN')
            """, new { email });

        if (row == null) return null;

        string storedHash = row.password_hash ?? "";
        if (!string.Equals(storedHash, hash, StringComparison.OrdinalIgnoreCase))
            return null;

        return new Employee
        {
            IdStaff = row.id_staff,
            Nombre = row.nombre,
            Email = row.email,
            Rol = row.nombre_rol
        };
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
