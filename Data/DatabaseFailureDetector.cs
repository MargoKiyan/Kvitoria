using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Kvitoria.Data;

public static class DatabaseFailureDetector
{
    public static bool IsDatabaseUnavailable(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException or NpgsqlException or SocketException or TimeoutException)
            {
                return true;
            }

            if (current is DbUpdateException)
            {
                return true;
            }

            if (current.Message.Contains("Npgsql", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("database", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("connection", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
