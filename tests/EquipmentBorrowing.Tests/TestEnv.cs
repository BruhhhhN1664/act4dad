using EquipmentBorrowing.Application;
using EquipmentBorrowing.Application.Abstractions;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Infrastructure;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace EquipmentBorrowing.Tests;

public sealed class FixedClock : IClock
{
    public DateOnly Today { get; set; } = new(2026, 3, 2);
}


public sealed class TestEnv : IAsyncDisposable
{
    public static string? Connection => Environment.GetEnvironmentVariable("EQUIPMENT_TEST_CONNECTION");

    public required IDbContextFactory<EquipmentBorrowingDbContext> Factory { get; init; }
    public required StudentService Students { get; init; }
    public required EquipmentService Equipment { get; init; }
    public required BorrowingService Borrowing { get; init; }
    public required FixedClock Clock { get; init; }
    public required DemoFaultSwitch Fault { get; init; }
    private ServiceProvider _provider = null!;

    public static async Task<TestEnv> CreateAsync()
    {
        Skip.If(string.IsNullOrWhiteSpace(Connection),
            "Set EQUIPMENT_TEST_CONNECTION to a disposable PostgreSQL database to run these tests.");

        var services = new ServiceCollection().AddPersistence(Connection!);
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IDbContextFactory<EquipmentBorrowingDbContext>>();

        NpgsqlConnection.ClearAllPools();
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            var uows = provider.GetRequiredService<IUnitOfWorkFactory>();
            var classifier = provider.GetRequiredService<IDatabaseErrorClassifier>();
            var clock = new FixedClock();
            var fault = new DemoFaultSwitch();
            return new TestEnv
            {
                Factory = factory,
                Students = new StudentService(uows, classifier),
                Equipment = new EquipmentService(uows, classifier),
                Borrowing = new BorrowingService(uows, classifier, clock, fault),
                Clock = clock,
                Fault = fault,
                _provider = provider
            };
        }
    }

    public async Task<int> AddStudentAsync(string number, string name = "Test Student")
    {
        var r = await Students.RegisterAsync(number, name, null);
        Assert.True(r.Succeeded, r.Message);
        return r.Value!.Id;
    }

    public async Task<int> AddItemAsync(string id, string name = "Test Item")
    {
        var r = await Equipment.RegisterAsync(id, name, null);
        Assert.True(r.Succeeded, r.Message);
        return r.Value!.Id;
    }

    public DateOnly Due(int days = 7) => Clock.Today.AddDays(days);

    public async ValueTask DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await _provider.DisposeAsync();
    }
}
