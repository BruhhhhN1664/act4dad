namespace EquipmentBorrowing.Domain;


public static class TextRules
{
    public static string Normalize(string value) => value.Trim().ToUpperInvariant();

    public static string? CleanOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
