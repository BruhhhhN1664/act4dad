using EquipmentBorrowing.Application.Abstractions;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

internal static class Like
{
    public static string Contains(string text)
        => "%" + text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
}

internal sealed class StudentRepository : IStudentRepository
{
    private readonly EquipmentBorrowingDbContext _db;
    public StudentRepository(EquipmentBorrowingDbContext db) => _db = db;

    public async Task<IReadOnlyList<Student>> SearchAsync(string? text, CancellationToken ct)
    {
        IQueryable<Student> q = _db.Students.AsNoTracking();
        var t = text?.Trim();
        if (!string.IsNullOrEmpty(t))
        {
            var p = Like.Contains(t);
            q = q.Where(s => EF.Functions.ILike(s.FullName, p, "\\") || EF.Functions.ILike(s.StudentNumber, p, "\\"));
        }
        return await q.OrderBy(s => s.FullName).ThenBy(s => s.Id).ToListAsync(ct);
    }

    public async Task<Student?> GetAsync(int id, bool forUpdate, CancellationToken ct)
    {
        if (!forUpdate)
            return await _db.Students.FirstOrDefaultAsync(s => s.Id == id, ct);

        var rows = await _db.Students
            .FromSqlInterpolated($"""SELECT * FROM "Students" WHERE "Id" = {id} FOR UPDATE""")
            .ToListAsync(ct);
        return rows.FirstOrDefault();
    }

    public Task<Student?> FindByNormalizedNumberAsync(string normalized, CancellationToken ct)
        => _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentNumberNormalized == normalized, ct);

    public void Add(Student student) => _db.Students.Add(student);
}

internal sealed class EquipmentRepository : IEquipmentRepository
{
    private readonly EquipmentBorrowingDbContext _db;
    public EquipmentRepository(EquipmentBorrowingDbContext db) => _db = db;

    public async Task<IReadOnlyList<Equipment>> SearchAsync(string? text, bool availableOnly, CancellationToken ct)
    {
        IQueryable<Equipment> q = _db.Equipment.AsNoTracking();
        if (availableOnly) q = q.Where(e => e.IsAvailable);
        var t = text?.Trim();
        if (!string.IsNullOrEmpty(t))
        {
            var p = Like.Contains(t);
            q = q.Where(e => EF.Functions.ILike(e.Name, p, "\\")
                          || EF.Functions.ILike(e.EquipmentIdentifier, p, "\\")
                          || (e.Description != null && EF.Functions.ILike(e.Description, p, "\\")));
        }
        return await q.OrderBy(e => e.Name).ThenBy(e => e.Id).ToListAsync(ct);
    }

    public Task<Equipment?> GetAsync(int id, CancellationToken ct)
        => _db.Equipment.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<Equipment>> GetForUpdateAsync(IReadOnlyCollection<int> ids, CancellationToken ct)
    {
        var arr = ids.ToArray();
        return await _db.Equipment
            .FromSqlInterpolated($"""SELECT * FROM "Equipment" WHERE "Id" = ANY({arr}) ORDER BY "Id" FOR UPDATE""")
            .ToListAsync(ct);
    }

    public Task<Equipment?> FindByNormalizedIdentifierAsync(string normalized, CancellationToken ct)
        => _db.Equipment.AsNoTracking().FirstOrDefaultAsync(e => e.EquipmentIdentifierNormalized == normalized, ct);

    public void Add(Equipment equipment) => _db.Equipment.Add(equipment);
}

internal sealed class BorrowingRepository : IBorrowingRepository
{
    private readonly EquipmentBorrowingDbContext _db;
    public BorrowingRepository(EquipmentBorrowingDbContext db) => _db = db;

    public Task<int> CountActiveForStudentAsync(int studentId, CancellationToken ct)
        => _db.Borrowings.CountAsync(b => b.StudentId == studentId && b.ReturnedAt == null, ct);

    public async Task<IReadOnlySet<int>> GetActiveEquipmentIdsAsync(IReadOnlyCollection<int> equipmentIds, CancellationToken ct)
    {
        var idList = equipmentIds.Distinct().ToList();
        if (idList.Count == 0)
            return new HashSet<int>();

        var found = await _db.Borrowings
            .AsNoTracking()
            .Where(b => b.ReturnedAt == null && idList.Contains(b.EquipmentId))
            .Select(b => b.EquipmentId)
            .ToListAsync(ct);

        return found.ToHashSet();
    }

    public async Task<Borrowing?> GetForUpdateAsync(int borrowingId, CancellationToken ct)
    {
        var rows = await _db.Borrowings
            .FromSqlInterpolated($"""SELECT * FROM "Borrowings" WHERE "Id" = {borrowingId} FOR UPDATE""")
            .ToListAsync(ct);
        return rows.FirstOrDefault();
    }

    public void Add(Borrowing borrowing) => _db.Borrowings.Add(borrowing);

    public async Task<IReadOnlyList<BorrowingRecord>> SearchAsync(BorrowingQuery query, CancellationToken ct)
    {
        IQueryable<Borrowing> q = _db.Borrowings.AsNoTracking();

        if (query.StudentId is int sid) q = q.Where(b => b.StudentId == sid);
        if (query.EquipmentId is int eid) q = q.Where(b => b.EquipmentId == eid);

        q = query.Status switch
        {
            StatusFilter.Active => q.Where(b => b.Status == BorrowingStatus.Active),
            StatusFilter.Returned => q.Where(b => b.Status == BorrowingStatus.Returned),
            _ => q
        };

        var t = query.Text?.Trim();
        if (!string.IsNullOrEmpty(t))
        {
            var p = Like.Contains(t);
            q = q.Where(b => EF.Functions.ILike(b.Student.FullName, p, "\\")
                          || EF.Functions.ILike(b.Student.StudentNumber, p, "\\")
                          || EF.Functions.ILike(b.Equipment.Name, p, "\\"));
        }

        IOrderedQueryable<Borrowing> ordered = (query.SortBy, query.Descending) switch
        {
            (SortField.ExpectedReturnDate, false) => q.OrderBy(b => b.ExpectedReturnAt),
            (SortField.ExpectedReturnDate, true) => q.OrderByDescending(b => b.ExpectedReturnAt),
            (_, false) => q.OrderBy(b => b.BorrowedAt),
            _ => q.OrderByDescending(b => b.BorrowedAt)
        };

        return await ordered.ThenBy(b => b.Id)
            .Select(b => new BorrowingRecord(
                b.Id, b.StudentId, b.Student.StudentNumber, b.Student.FullName,
                b.EquipmentId, b.Equipment.EquipmentIdentifier, b.Equipment.Name,
                b.BorrowedAt, b.ExpectedReturnAt, b.ReturnedAt, b.Status))
            .ToListAsync(ct);
    }
}
