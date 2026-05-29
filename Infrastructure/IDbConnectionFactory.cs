using System.Data;
using MySqlConnector;
using Npgsql;

namespace events_tickets.Infrastructure;

public interface IDbConnectionFactory
{
    IDbConnection Create();
}

public class PostgresConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public PostgresConnectionFactory(string connectionString) =>
        _connectionString = connectionString;

    public IDbConnection Create() => new NpgsqlConnection(_connectionString);
}

public class MySqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public MySqlConnectionFactory(string connectionString) =>
        _connectionString = connectionString;

    public IDbConnection Create() => new MySqlConnection(_connectionString);
}
