using EquipmentBorrowing.Application.Abstractions;
using EquipmentBorrowing.Domain;
using CheckoutResult = EquipmentBorrowing.Application.Result<System.Collections.Generic.IReadOnlyList<int>>;
using RecordsResult = EquipmentBorrowing.Application.Result<System.Collections.Generic.IReadOnlyList<EquipmentBorrowing.Application.Abstractions.BorrowingRecord>>;

namespace EquipmentBorrowing.Application.Services;

public sealed class BorrowingService : ServiceBase
{
    private readonly IClock _clock;
    private readonly ITransactionFaultHook _fault;

    public BorrowingService(IUnitOfWorkFactory uows, IDatabaseErrorClassifier classifier, IClock clock, ITransactionFaultHook fault)
        : base(uows, classifier)
    {
        _clock = clock;
        _fault = fault;
    }


    public Task<CheckoutResult> BorrowAsync(int studentId, int equipmentId, DateOnly? expectedReturn, CancellationToken ct = default)
        => CheckoutCoreAsync(studentId, new[] { equipmentId }, expectedReturn, minItems: 1, ct);

    public Task<CheckoutResult> CheckoutMultipleAsync(int studentId, IReadOnlyCollection<int> equipmentIds, DateOnly? expectedReturn, CancellationToken ct = default)
        => CheckoutCoreAsync(studentId, equipmentIds, expectedReturn, minItems: 2, ct);

    private Task<CheckoutResult> CheckoutCoreAsync(int studentId, IReadOnlyCollection<int> equipmentIds, DateOnly? expectedReturn, int minItems, CancellationToken ct)
        => GuardAsync<IReadOnlyList<int>>(async () =>
        {
            var ids = equipmentIds.ToList();
            if (ids.Count < minItems)
                return CheckoutResult.Fail(ErrorKind.Validation, minItems > 1
                    ? "A multi-item checkout needs at least two different items. Nothing was recorded."
                    : "Select the item to borrow.");
            if (ids.Distinct().Count() != ids.Count)
                return CheckoutResult.Fail(ErrorKind.Validation, "The same item was selected more than once. Nothing was recorded.");
            if (expectedReturn is null)
                return CheckoutResult.Fail(ErrorKind.Validation, "Enter an expected return date.");

            var today = _clock.Today;
            if (expectedReturn.Value <= today)
                return CheckoutResult.Fail(ErrorKind.Validation,
                    $"The expected return date must be later than the checkout date ({today:yyyy-MM-dd}).");

            await using var uow = await Uows.CreateAsync(ct);
            await uow.BeginTransactionAsync(ct);

            var student = await uow.Students.GetAsync(studentId, forUpdate: true, ct);
            if (student is null)
                return CheckoutResult.Fail(ErrorKind.NotFound, "The selected student no longer exists. Refresh the list.");
            if (!student.IsAllowedToBorrow)
                return CheckoutResult.Fail(ErrorKind.Rejected, $"{student.FullName} is not allowed to borrow equipment. Nothing was recorded.");

            var items = await uow.Equipment.GetForUpdateAsync(ids, ct);
            if (items.Count != ids.Count)
                return CheckoutResult.Fail(ErrorKind.NotFound, "One or more selected items no longer exist. Refresh the list. Nothing was recorded.");

            var activeIds = await uow.Borrowings.GetActiveEquipmentIdsAsync(ids, ct);
            var unavailable = items.Where(e => !e.IsAvailable || activeIds.Contains(e.Id)).ToList();
            if (unavailable.Count > 0)
                return CheckoutResult.Fail(ErrorKind.Rejected,
                    "Not available: " + string.Join(", ", unavailable.Select(e => e.EquipmentIdentifier)) + ". Nothing was recorded.");

            var activeCount = await uow.Borrowings.CountActiveForStudentAsync(studentId, ct);
            if (activeCount + ids.Count > BorrowingRules.MaxActiveBorrowingsPerStudent)
                return CheckoutResult.Fail(ErrorKind.Rejected,
                    $"{student.FullName} has {activeCount} active borrowing(s); the limit is {BorrowingRules.MaxActiveBorrowingsPerStudent} " +
                    $"and {ids.Count} more were requested. Nothing was recorded.");

            var created = new List<Borrowing>();
            var first = true;
            foreach (var item in items)
            {
                var borrowing = Borrowing.Issue(studentId, item.Id, today, expectedReturn.Value);
                item.MarkIssued();
                uow.Borrowings.Add(borrowing);
                created.Add(borrowing);
                await uow.SaveChangesAsync(ct);
                if (first) { first = false; _fault.AfterFirstWrite(); }
            }

            await CommitAsync(uow, ct);
            var message = created.Count == 1
                ? $"{items[0].Name} issued to {student.FullName}, due {expectedReturn.Value:yyyy-MM-dd}."
                : $"{created.Count} items issued to {student.FullName}, due {expectedReturn.Value:yyyy-MM-dd}.";
            return CheckoutResult.Ok(created.Select(b => b.Id).ToList(), message);
        });

    public Task<Result<int>> ReturnAsync(int borrowingId, CancellationToken ct = default)
        => GuardAsync(async () =>
        {
            await using var uow = await Uows.CreateAsync(ct);
            await uow.BeginTransactionAsync(ct);

            var borrowing = await uow.Borrowings.GetForUpdateAsync(borrowingId, ct);
            if (borrowing is null)
                return Result<int>.Fail(ErrorKind.NotFound, "That borrowing no longer exists. Refresh the list.");
            if (borrowing.Status != BorrowingStatus.Active)
                return Result<int>.Fail(ErrorKind.Rejected,
                    $"That borrowing was already returned on {borrowing.ReturnedAt:yyyy-MM-dd}. No change was made.");

            var today = _clock.Today;
            if (today < borrowing.BorrowedAt)
                return Result<int>.Fail(ErrorKind.Rejected,
                    $"The return date ({today:yyyy-MM-dd}) cannot be earlier than the checkout date ({borrowing.BorrowedAt:yyyy-MM-dd}). Check the computer's clock.");

            var item = (await uow.Equipment.GetForUpdateAsync(new[] { borrowing.EquipmentId }, ct)).FirstOrDefault();
            if (item is null)
                return Result<int>.Fail(ErrorKind.NotFound, "The borrowed equipment record is missing. Refresh the list.");

            borrowing.MarkReturned(today);
            item.MarkReturned();
            await uow.SaveChangesAsync(ct);
            await CommitAsync(uow, ct);
            return Result<int>.Ok(borrowing.Id, $"{item.Name} returned on {today:yyyy-MM-dd}.");
        });


    public Task<RecordsResult> GetActiveAsync(CancellationToken ct = default)
        => SearchAsync(new BorrowingQuery(Status: StatusFilter.Active, SortBy: SortField.ExpectedReturnDate, Descending: false), ct);

    public Task<RecordsResult> GetStudentHistoryAsync(int studentId, CancellationToken ct = default)
        => GuardAsync<IReadOnlyList<BorrowingRecord>>(async () =>
        {
            await using var uow = await Uows.CreateAsync(ct);
            if (await uow.Students.GetAsync(studentId, forUpdate: false, ct) is null)
                return RecordsResult.Fail(ErrorKind.NotFound, "That student no longer exists. Refresh the list.");
            var rows = await uow.Borrowings.SearchAsync(new BorrowingQuery(StudentId: studentId), ct);
            return RecordsResult.Ok(rows);
        });

    public Task<RecordsResult> GetEquipmentHistoryAsync(int equipmentId, CancellationToken ct = default)
        => GuardAsync<IReadOnlyList<BorrowingRecord>>(async () =>
        {
            await using var uow = await Uows.CreateAsync(ct);
            if (await uow.Equipment.GetAsync(equipmentId, ct) is null)
                return RecordsResult.Fail(ErrorKind.NotFound, "That equipment no longer exists. Refresh the list.");
            var rows = await uow.Borrowings.SearchAsync(new BorrowingQuery(EquipmentId: equipmentId), ct);
            return RecordsResult.Ok(rows);
        });

    public Task<RecordsResult> SearchAsync(BorrowingQuery query, CancellationToken ct = default)
        => GuardAsync<IReadOnlyList<BorrowingRecord>>(async () =>
        {
            await using var uow = await Uows.CreateAsync(ct);
            var rows = await uow.Borrowings.SearchAsync(query, ct);
            return RecordsResult.Ok(rows);
        });
}
