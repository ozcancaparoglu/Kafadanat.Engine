using System.Data;
using Npgsql;
using Onspay.Cqrs.Data;

namespace Infrastructure.Database;

internal sealed class SqlConnectionFactory(string connectionString) : ISqlConnectionFactory
{
    public IDbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        return connection;
    }
}
