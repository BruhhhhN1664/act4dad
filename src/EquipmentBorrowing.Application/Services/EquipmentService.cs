using EquipmentBorrowing.Application.Abstractions;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public sealed class EquipmentService : ServiceBase
{
    public const int MaxIdentifierLength = 30, MaxNameLength = 100, MaxDescriptionLength = 200;

    public EquipmentService(IUnitOfWorkFactory uows, IDatabaseErrorClassifier classifier) : base(uows, classifier) { }

    public Task<Result<IReadOnlyList<Equipment>>> SearchAsync(string? text, bool availableOnly, CancellationToken ct = default)
        => GuardAsync<IReadOnlyList<Equipment>>(async () =>
        {
            await using var uow = await Uows.CreateAsync(ct);
            var list = await uow.Equipment.SearchAsync(text, availableOnly, ct);
            return Result<IReadOnlyList<Equipment>>.Ok(list);
        });

    public Task<Result<Equipment>> RegisterAsync(string? identifier, string? name, string? description, CancellationToken ct = default)
        => GuardAsync(async () =>
        {
            var invalid = Validate(identifier, name, description);
            if (invalid is not null) return Result<Equipment>.Fail(ErrorKind.Validation, invalid);

            await using var uow = await Uows.CreateAsync(ct);
            await uow.BeginTransactionAsync(ct);
            if (await uow.Equipment.FindByNormalizedIdentifierAsync(TextRules.Normalize(identifier!), ct) is not null)
                return Result<Equipment>.Fail(ErrorKind.Validation,
                    $"Equipment identifier '{identifier!.Trim()}' already exists (identifiers are compared ignoring case and spaces).");

            var item = Equipment.Register(identifier!, name!, description);
            uow.Equipment.Add(item);
            await uow.SaveChangesAsync(ct);
            await CommitAsync(uow, ct);
            return Result<Equipment>.Ok(item, $"Registered {item.Name} ({item.EquipmentIdentifier}). It is available.");
        });

    public Task<Result<Equipment>> UpdateAsync(int id, string? identifier, string? name, string? description, CancellationToken ct = default)
        => GuardAsync(async () =>
        {
            var invalid = Validate(identifier, name, description);
            if (invalid is not null) return Result<Equipment>.Fail(ErrorKind.Validation, invalid);

            await using var uow = await Uows.CreateAsync(ct);
            await uow.BeginTransactionAsync(ct);
            var locked = await uow.Equipment.GetForUpdateAsync(new[] { id }, ct);
            var item = locked.FirstOrDefault();
            if (item is null)
                return Result<Equipment>.Fail(ErrorKind.NotFound, "That equipment no longer exists. Refresh the list.");

            var other = await uow.Equipment.FindByNormalizedIdentifierAsync(TextRules.Normalize(identifier!), ct);
            if (other is not null && other.Id != id)
                return Result<Equipment>.Fail(ErrorKind.Validation,
                    $"Identifier '{identifier!.Trim()}' belongs to another item.");

            item.Update(identifier!, name!, description);
            await uow.SaveChangesAsync(ct);
            await CommitAsync(uow, ct);
            return Result<Equipment>.Ok(item, "Equipment details saved.");
        });

    private static string? Validate(string? identifier, string? name, string? description)
    {
        if (string.IsNullOrWhiteSpace(identifier)) return "Enter an equipment identifier.";
        if (string.IsNullOrWhiteSpace(name)) return "Enter the equipment name.";
        if (identifier.Trim().Length > MaxIdentifierLength) return $"Identifier is limited to {MaxIdentifierLength} characters.";
        if (name.Trim().Length > MaxNameLength) return $"Name is limited to {MaxNameLength} characters.";
        if ((description?.Trim().Length ?? 0) > MaxDescriptionLength) return $"Description is limited to {MaxDescriptionLength} characters.";
        return null;
    }
}
