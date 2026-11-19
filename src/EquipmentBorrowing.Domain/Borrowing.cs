namespace EquipmentBorrowing.Domain;

public enum BorrowingStatus { Active, Returned }

public class Borrowing
{
    public int Id { get; private set; }
    public int StudentId { get; private set; }
    public int EquipmentId { get; private set; }
    public DateOnly BorrowedAt { get; private set; }
    public DateOnly ExpectedReturnAt { get; private set; }
    public DateOnly? ReturnedAt { get; private set; }
    public BorrowingStatus Status { get; private set; }

    public Student Student { get; private set; } = null!;
    public Equipment Equipment { get; private set; } = null!;

    private Borrowing() { }

    public static Borrowing Issue(int studentId, int equipmentId, DateOnly borrowedAt, DateOnly expectedReturnAt)
        => new()
        {
            StudentId = studentId,
            EquipmentId = equipmentId,
            BorrowedAt = borrowedAt,
            ExpectedReturnAt = expectedReturnAt,
            Status = BorrowingStatus.Active
        };

    public void MarkReturned(DateOnly returnedAt)
    {
        ReturnedAt = returnedAt;
        Status = BorrowingStatus.Returned;
    }
}
