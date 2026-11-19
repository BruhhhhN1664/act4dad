using EquipmentBorrowing.Application.Abstractions;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EquipmentBorrowing.Infrastructure.Repositories;

internal sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly EquipmentBorrowingDbContext _db;
    private IDbContextTransaction? _tx;

    public EfUnitOfWork(EquipmentBorrowingDbContext db)
    {
        _db = db;
        Students = new StudentRepository(db);
        Equipment = new EquipmentRepository(db);
        Borrowings = new BorrowingRepository(db);
    }

    public IStudentRepository Students { get; }
    public IEquipmentRepository Equipment { get; }
    public IBorrowingRepository Borrowings { get; }

    public async Task BeginTransactionAsync(CancellationToken ct)
        => _tx = await _db.Database.BeginTransactionAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    public async Task CommitAsync(CancellationToken ct)
    {
        if (_tx is null) throw new InvalidOperationException("No transaction is open.");
        await _tx.CommitAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_tx is not null) await _tx.DisposeAsync();
        await _db.DisposeAsync();
    }
}

public sealed class EfUnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IDbContextFactory<EquipmentBorrowingDbContext> _factory;
    public EfUnitOfWorkFactory(IDbContextFactory<EquipmentBorrowingDbContext> factory) => _factory = factory;

    public async Task<IUnitOfWork> CreateAsync(CancellationToken ct)
        => new EfUnitOfWork(await _factory.CreateDbContextAsync(ct));
}
