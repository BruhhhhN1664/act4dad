using EquipmentBorrowing.Application.Abstractions;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public sealed class StudentService : ServiceBase
{
    public const int MaxNumberLength = 20, MaxNameLength = 100, MaxContactLength = 100;

    public StudentService(IUnitOfWorkFactory uows, IDatabaseErrorClassifier classifier) : base(uows, classifier) { }

    public Task<Result<IReadOnlyList<Student>>> SearchAsync(string? text, CancellationToken ct = default)
        => GuardAsync<IReadOnlyList<Student>>(async () =>
        {
            await using var uow = await Uows.CreateAsync(ct);
            var list = await uow.Students.SearchAsync(text, ct);
            return Result<IReadOnlyList<Student>>.Ok(list);
        });

    public Task<Result<Student>> RegisterAsync(string? number, string? name, string? contact, CancellationToken ct = default)
        => GuardAsync(async () =>
        {
            var invalid = Validate(number, name, contact);
            if (invalid is not null) return Result<Student>.Fail(ErrorKind.Validation, invalid);

            await using var uow = await Uows.CreateAsync(ct);
            await uow.BeginTransactionAsync(ct);
            var normalized = TextRules.Normalize(number!);
            if (await uow.Students.FindByNormalizedNumberAsync(normalized, ct) is not null)
                return Result<Student>.Fail(ErrorKind.Validation,
                    $"Student number '{number!.Trim()}' is already registered (numbers are compared ignoring case and spaces).");

            var student = Student.Register(number!, name!, contact);
            uow.Students.Add(student);
            await uow.SaveChangesAsync(ct);
            await CommitAsync(uow, ct);
            return Result<Student>.Ok(student, $"Registered {student.FullName} ({student.StudentNumber}). Eligible to borrow.");
        });

    public Task<Result<Student>> UpdateAsync(int id, string? number, string? name, string? contact, CancellationToken ct = default)
        => GuardAsync(async () =>
        {
            var invalid = Validate(number, name, contact);
            if (invalid is not null) return Result<Student>.Fail(ErrorKind.Validation, invalid);

            await using var uow = await Uows.CreateAsync(ct);
            await uow.BeginTransactionAsync(ct);
            var student = await uow.Students.GetAsync(id, forUpdate: true, ct);
            if (student is null)
                return Result<Student>.Fail(ErrorKind.NotFound, "That student no longer exists. Refresh the list.");

            var normalized = TextRules.Normalize(number!);
            var other = await uow.Students.FindByNormalizedNumberAsync(normalized, ct);
            if (other is not null && other.Id != id)
                return Result<Student>.Fail(ErrorKind.Validation,
                    $"Student number '{number!.Trim()}' belongs to another student.");

            student.Update(number!, name!, contact);
            await uow.SaveChangesAsync(ct);
            await CommitAsync(uow, ct);
            return Result<Student>.Ok(student, "Student details saved.");
        });

    public Task<Result<bool>> SetEligibilityAsync(int id, bool allowed, CancellationToken ct = default)
        => GuardAsync(async () =>
        {
            await using var uow = await Uows.CreateAsync(ct);
            await uow.BeginTransactionAsync(ct);
            var student = await uow.Students.GetAsync(id, forUpdate: true, ct);
            if (student is null)
                return Result<bool>.Fail(ErrorKind.NotFound, "That student no longer exists. Refresh the list.");

            if (student.IsAllowedToBorrow == allowed)
                return Result<bool>.Ok(false,
                    $"{student.FullName} is already {(allowed ? "allowed" : "not allowed")} to borrow. No change was made.");

            student.SetEligibility(allowed);
            await uow.SaveChangesAsync(ct);
            await CommitAsync(uow, ct);
            return Result<bool>.Ok(true, allowed
                ? $"{student.FullName} may borrow equipment again."
                : $"{student.FullName} can no longer borrow new equipment. Existing borrowings can still be returned.");
        });

    private static string? Validate(string? number, string? name, string? contact)
    {
        if (string.IsNullOrWhiteSpace(number)) return "Enter a student number.";
        if (string.IsNullOrWhiteSpace(name)) return "Enter the student's full name.";
        if (number.Trim().Length > MaxNumberLength) return $"Student number is limited to {MaxNumberLength} characters.";
        if (name.Trim().Length > MaxNameLength) return $"Name is limited to {MaxNameLength} characters.";
        if ((contact?.Trim().Length ?? 0) > MaxContactLength) return $"Contact is limited to {MaxContactLength} characters.";
        return null;
    }
}
