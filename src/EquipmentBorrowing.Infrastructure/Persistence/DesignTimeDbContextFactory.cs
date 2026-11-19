using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public static class ConnectionDefaults
{
    public const string Default = "Host=localhost;Port=5432;Database=equipment_borrowing;Username=postgres";
}

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<EquipmentBorrowingDbContext>
{
    public EquipmentBorrowingDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres") ?? ConnectionDefaults.Default;
        var options = new DbContextOptionsBuilder<EquipmentBorrowingDbContext>().UseNpgsql(cs).Options;
        return new EquipmentBorrowingDbContext(options);
    }
}
