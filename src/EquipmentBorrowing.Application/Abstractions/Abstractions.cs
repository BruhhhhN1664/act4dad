using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Abstractions;

public interface IStudentRepository
{
    Task<IReadOnlyList<Student>> SearchAsync(string? text, CancellationToken ct);
    Task<Student?> GetAsync(int id, bool forUpdate, CancellationToken ct);
    Task<Student?> FindByNormalizedNumberAsync(string normalized, CancellationToken ct);
    void Add(Student student);
}

public interface IEquipmentRepository
{
    Task<IReadOnlyList<Equipment>> SearchAsync(string? text, bool availableOnly, CancellationToken ct);
    Task<Equipment?> GetAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<Equipment>> GetForUpdateAsync(IReadOnlyCollection<int> ids, CancellationToken ct);
    Task<Equipment?> FindByNormalizedIdentifierAsync(string normalized, CancellationToken ct);
    void Add(Equipment equipment);
}

public interface IBorrowingRepository
{
    Task<int> CountActiveForStudentAsync(int studentId, CancellationToken ct);
    Task<IReadOnlySet<int>> GetActiveEquipmentIdsAsync(IReadOnlyCollection<int> equipmentIds, CancellationToken ct);
    Task<Borrowing?> GetForUpdateAsync(int borrowingId, CancellationToken ct);
    void Add(Borrowing borrowing);
    Task<IReadOnlyList<BorrowingRecord>> SearchAsync(BorrowingQuery query, CancellationToken ct);
}

public interface IUnitOfWork : IAsyncDisposable
{
    IStudentRepository Students { get; }
    IEquipmentRepository Equipment { get; }
    IBorrowingRepository Borrowings { get; }
    Task BeginTransactionAsync(CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
    Task CommitAsync(CancellationToken ct);
}

public interface IUnitOfWorkFactory
{
    Task<IUnitOfWork> CreateAsync(CancellationToken ct);
}

public enum DbFailureKind { Other, Unavailable, UniqueViolation, ForeignKeyViolation, CheckViolation }

public interface IDatabaseErrorClassifier
{
    DbFailureKind Classify(Exception exception);
}

public interface IDatabaseInitializer
{
    Task<Result<bool>> InitializeAsync(CancellationToken ct);
}

public interface IClock
{
    DateOnly Today { get; }
}

public sealed class SystemClock : IClock
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}

public interface ITransactionFaultHook
{
    void AfterFirstWrite();
}

public sealed class SimulatedFaultException : Exception
{
    public SimulatedFaultException() : base("Simulated failure after the first write.") { }
}

public sealed class DemoFaultSwitch : ITransactionFaultHook
{
    public bool Enabled { get; set; }
    public void AfterFirstWrite()
    {
        if (Enabled) throw new SimulatedFaultException();
    }
}

public sealed class CommitOutcomeUnknownException : Exception
{
    public CommitOutcomeUnknownException(Exception inner) : base("Commit outcome unknown.", inner) { }
}

public enum StatusFilter { All, Active, Returned }
public enum SortField { CheckoutDate, ExpectedReturnDate }

public record BorrowingQuery(
    string? Text = null,
    StatusFilter Status = StatusFilter.All,
    SortField SortBy = SortField.CheckoutDate,
    bool Descending = true,
    int? StudentId = null,
    int? EquipmentId = null);

public record BorrowingRecord(
    int Id,
    int StudentId,
    string StudentNumber,
    string StudentName,
    int EquipmentId,
    string EquipmentIdentifier,
    string EquipmentName,
    DateOnly BorrowedAt,
    DateOnly ExpectedReturnAt,
    DateOnly? ReturnedAt,
    BorrowingStatus Status)
{
    public string BorrowedText => BorrowedAt.ToString("yyyy-MM-dd");
    public string ExpectedText => ExpectedReturnAt.ToString("yyyy-MM-dd");
    public string ReturnedText => ReturnedAt?.ToString("yyyy-MM-dd") ?? "—";
    public string StatusText => Status.ToString();
}
