using EquipmentBorrowing.Application.Abstractions;
using Npgsql;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public sealed class NpgsqlErrorClassifier : IDatabaseErrorClassifier
{
    public DbFailureKind Classify(Exception exception)
    {
        for (var e = exception; e is not null; e = e.InnerException)
        {
            if (e is PostgresException pg)
            {
                var state = pg.SqlState;
                if (state == "23505") return DbFailureKind.UniqueViolation;
                if (state == "23503") return DbFailureKind.ForeignKeyViolation;
                if (state == "23514" || state == "23502") return DbFailureKind.CheckViolation;
                if (state.StartsWith("08") || state.StartsWith("28") || state == "3D000" || state.StartsWith("57P"))
                    return DbFailureKind.Unavailable;
                return DbFailureKind.Other;
            }
            if (e is NpgsqlException or System.Net.Sockets.SocketException or System.IO.IOException or TimeoutException)
                return DbFailureKind.Unavailable;
        }
        return DbFailureKind.Other;
    }
}
