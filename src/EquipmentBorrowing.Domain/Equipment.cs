namespace EquipmentBorrowing.Domain;

public class Equipment
{
    public int Id { get; private set; }
    public string EquipmentIdentifier { get; private set; } = "";
    public string EquipmentIdentifierNormalized { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string? Description { get; private set; }
    public bool IsAvailable { get; private set; } = true;

    public string AvailabilityText => IsAvailable ? "Available" : "Borrowed";

    private Equipment() { }

    public static Equipment Register(string identifier, string name, string? description)
    {
        var e = new Equipment { IsAvailable = true };
        e.Apply(identifier, name, description);
        return e;
    }

    public void Update(string identifier, string name, string? description) => Apply(identifier, name, description);

    public void MarkIssued() => IsAvailable = false;
    public void MarkReturned() => IsAvailable = true;

    private void Apply(string identifier, string name, string? description)
    {
        EquipmentIdentifier = identifier.Trim();
        EquipmentIdentifierNormalized = TextRules.Normalize(EquipmentIdentifier);
        Name = name.Trim();
        Description = TextRules.CleanOptional(description);
    }
}
