using EquipmentBorrowing.Application;
using EquipmentBorrowing.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IDbContextFactory<EquipmentBorrowingDbContext> _factory;
    private readonly IDatabaseErrorClassifier _classifier;

    public DatabaseInitializer(IDbContextFactory<EquipmentBorrowingDbContext> factory, IDatabaseErrorClassifier classifier)
    {
        _factory = factory;
        _classifier = classifier;
    }

    public async Task<Result<bool>> InitializeAsync(CancellationToken ct)
    {
        try
        {
            await using var db = await _factory.CreateDbContextAsync(ct);
            if (!db.Database.GetMigrations().Any())
                return Result<bool>.Fail(ErrorKind.Unexpected,
                    "No EF Core migrations were found. Run the setup script (or `dotnet ef migrations add InitialPostgres`) once, then restart.");

            await db.Database.MigrateAsync(ct);
            await SeedData.SeedIfEmptyAsync(db, ct);
            return Result<bool>.Ok(true, "Database ready.");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            return _classifier.Classify(ex) == DbFailureKind.Unavailable
                ? Result<bool>.Fail(ErrorKind.ConnectionError,
                    "Cannot reach the PostgreSQL database. Check that the server is running, the database exists and the credentials are right (see README), then press Retry.")
                : Result<bool>.Fail(ErrorKind.Unexpected,
                    "The database could not be prepared (migration or seed failed). Nothing in the app will work until this is fixed; see README troubleshooting.");
        }
    }
}
