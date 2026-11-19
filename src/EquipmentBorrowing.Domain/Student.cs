namespace EquipmentBorrowing.Domain;

public class Student
{
    public int Id { get; private set; }
    public string StudentNumber { get; private set; } = "";
    public string StudentNumberNormalized { get; private set; } = "";
    public string FullName { get; private set; } = "";
    public string? Contact { get; private set; }
    public bool IsAllowedToBorrow { get; private set; } = true;

    public string EligibilityText => IsAllowedToBorrow ? "Allowed" : "Not allowed";

    private Student() { }

    public static Student Register(string studentNumber, string fullName, string? contact)
    {
        var s = new Student { IsAllowedToBorrow = true };
        s.Apply(studentNumber, fullName, contact);
        return s;
    }

    public void Update(string studentNumber, string fullName, string? contact) => Apply(studentNumber, fullName, contact);

    public void SetEligibility(bool allowed) => IsAllowedToBorrow = allowed;

    private void Apply(string studentNumber, string fullName, string? contact)
    {
        StudentNumber = studentNumber.Trim();
        StudentNumberNormalized = TextRules.Normalize(StudentNumber);
        FullName = fullName.Trim();
        Contact = TextRules.CleanOptional(contact);
    }
}
