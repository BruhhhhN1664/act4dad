using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Persistence;

internal static class SeedData
{
    public static async Task SeedIfEmptyAsync(EquipmentBorrowingDbContext db, CancellationToken ct)
    {
        if (await db.Students.AnyAsync(ct) || await db.Equipment.AnyAsync(ct)) return;

        var ana = Student.Register("2021-0001", "Ana Reyes", "ana.reyes@example.edu");
        var ben = Student.Register("2021-0002", "Ben Cruz", null);
        var carla = Student.Register("2021-0003", "Carla Santos", "0917 000 0003");
        carla.SetEligibility(false);
        var diego = Student.Register("2021-0004", "Diego Ramos", null);
        var elena = Student.Register("2021-0005", "Elena Flores", null);
        db.Students.AddRange(ana, ben, carla, diego, elena);

        var lap1 = Equipment.Register("LAP-01", "Laptop 01", "14-inch laptop");
        var lap2 = Equipment.Register("LAP-02", "Laptop 02", "14-inch laptop");
        var cam = Equipment.Register("CAM-01", "Camera 01", "DSLR camera with lens");
        var prj = Equipment.Register("PRJ-01", "Projector 01", null);
        var tri = Equipment.Register("TRI-01", "Tripod 01", null);
        var mic = Equipment.Register("MIC-01", "Microphone 01", "USB microphone");
        db.Equipment.AddRange(lap1, lap2, cam, prj, tri, mic);
        await db.SaveChangesAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.Now);

        var active = Borrowing.Issue(ben.Id, cam.Id, today.AddDays(-2), today.AddDays(5));
        cam.MarkIssued();

        var done = Borrowing.Issue(ana.Id, prj.Id, today.AddDays(-14), today.AddDays(-7));
        done.MarkReturned(today.AddDays(-8));

        db.Borrowings.AddRange(active, done);
        await db.SaveChangesAsync(ct);
    }
}
