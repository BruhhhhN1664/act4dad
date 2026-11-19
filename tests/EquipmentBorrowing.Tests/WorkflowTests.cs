using EquipmentBorrowing.Application;
using EquipmentBorrowing.Application.Abstractions;
using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EquipmentBorrowing.Tests;

public class WorkflowTests
{
    [SkippableFact]
    public async Task Duplicate_student_number_is_rejected_ignoring_case_and_spaces()
    {
        await using var env = await TestEnv.CreateAsync();
        await env.AddStudentAsync("S-001");
        var dup = await env.Students.RegisterAsync("  s-001 ", "Someone Else", null);
        Assert.False(dup.Succeeded);
        Assert.Equal(ErrorKind.Validation, dup.Kind);
        Assert.Single((await env.Students.SearchAsync(null)).Value!);
    }

    [SkippableFact]
    public async Task Duplicate_equipment_identifier_is_rejected_ignoring_case_and_spaces()
    {
        await using var env = await TestEnv.CreateAsync();
        await env.AddItemAsync("LAP-01");
        var dup = await env.Equipment.RegisterAsync(" lap-01", "Another", null);
        Assert.False(dup.Succeeded);
        Assert.Equal(ErrorKind.Validation, dup.Kind);
    }

    [SkippableFact]
    public async Task Blank_required_fields_are_rejected_and_blank_optionals_become_null()
    {
        await using var env = await TestEnv.CreateAsync();
        Assert.False((await env.Students.RegisterAsync("  ", "Name", null)).Succeeded);
        Assert.False((await env.Equipment.RegisterAsync("ID-1", " ", null)).Succeeded);
        var ok = await env.Students.RegisterAsync("S-9", "Name", "   ");
        Assert.True(ok.Succeeded);
        Assert.Null(ok.Value!.Contact);
    }

    [SkippableFact]
    public async Task Single_borrow_is_stored_and_visible_from_a_fresh_context()
    {
        await using var env = await TestEnv.CreateAsync();
        var s = await env.AddStudentAsync("S-1");
        var e = await env.AddItemAsync("E-1");

        var r = await env.Borrowing.BorrowAsync(s, e, env.Due());
        Assert.True(r.Succeeded, r.Message);

        await using var db = await env.Factory.CreateDbContextAsync();
        Assert.Equal(1, await db.Borrowings.CountAsync(b => b.ReturnedAt == null));
        Assert.False((await db.Equipment.SingleAsync()).IsAvailable);
    }

    [SkippableFact]
    public async Task Expected_return_must_be_after_checkout_date_and_present()
    {
        await using var env = await TestEnv.CreateAsync();
        var s = await env.AddStudentAsync("S-1");
        var e = await env.AddItemAsync("E-1");
        Assert.False((await env.Borrowing.BorrowAsync(s, e, null)).Succeeded);
        Assert.False((await env.Borrowing.BorrowAsync(s, e, env.Clock.Today)).Succeeded);
        Assert.False((await env.Borrowing.BorrowAsync(s, e, env.Clock.Today.AddDays(-1))).Succeeded);
        Assert.True((await env.Borrowing.BorrowAsync(s, e, env.Clock.Today.AddDays(1))).Succeeded);
    }

    [SkippableFact]
    public async Task Restricted_student_cannot_borrow_but_can_still_return()
    {
        await using var env = await TestEnv.CreateAsync();
        var s = await env.AddStudentAsync("S-1");
        var e1 = await env.AddItemAsync("E-1");
        var e2 = await env.AddItemAsync("E-2");
        var borrow = await env.Borrowing.BorrowAsync(s, e1, env.Due());
        Assert.True(borrow.Succeeded);

        Assert.True((await env.Students.SetEligibilityAsync(s, false)).Value);
        var rejected = await env.Borrowing.BorrowAsync(s, e2, env.Due());
        Assert.False(rejected.Succeeded);
        Assert.Equal(ErrorKind.Rejected, rejected.Kind);

        var ret = await env.Borrowing.ReturnAsync(borrow.Value![0]);
        Assert.True(ret.Succeeded, ret.Message);
    }

    [SkippableFact]
    public async Task Repeating_an_eligibility_change_reports_no_change()
    {
        await using var env = await TestEnv.CreateAsync();
        var s = await env.AddStudentAsync("S-1");
        var again = await env.Students.SetEligibilityAsync(s, true);   // already allowed
        Assert.True(again.Succeeded);
        Assert.False(again.Value);
    }

    [SkippableFact]
    public async Task Unavailable_item_is_rejected()
    {
        await using var env = await TestEnv.CreateAsync();
        var a = await env.AddStudentAsync("S-1");
        var b = await env.AddStudentAsync("S-2");
        var e = await env.AddItemAsync("E-1");
        Assert.True((await env.Borrowing.BorrowAsync(a, e, env.Due())).Succeeded);
        var second = await env.Borrowing.BorrowAsync(b, e, env.Due());
        Assert.False(second.Succeeded);
        Assert.Equal(ErrorKind.Rejected, second.Kind);
    }

    [SkippableFact]
    public async Task Limit_of_three_is_enforced_including_exactly_at_the_limit()
    {
        await using var env = await TestEnv.CreateAsync();
        var s = await env.AddStudentAsync("S-1");
        var items = new List<int>();
        for (var i = 1; i <= 5; i++) items.Add(await env.AddItemAsync($"E-{i}"));

        Assert.True((await env.Borrowing.BorrowAsync(s, items[0], env.Due())).Succeeded);
        Assert.True((await env.Borrowing.CheckoutMultipleAsync(s, new[] { items[1], items[2] }, env.Due())).Succeeded);
        var over = await env.Borrowing.BorrowAsync(s, items[3], env.Due());
        Assert.False(over.Succeeded);
        Assert.Equal(ErrorKind.Rejected, over.Kind);

        var other = await env.AddStudentAsync("S-2");
        var four = await env.Borrowing.CheckoutMultipleAsync(other, new[] { items[3], items[4] }, env.Due());
        Assert.True(four.Succeeded);
    }

    [SkippableFact]
    public async Task Multi_checkout_needs_two_distinct_items_and_records_nothing_otherwise()
    {
        await using var env = await TestEnv.CreateAsync();
        var s = await env.AddStudentAsync("S-1");
        var e1 = await env.AddItemAsync("E-1");
        var e2 = await env.AddItemAsync("E-2");

        Assert.False((await env.Borrowing.CheckoutMultipleAsync(s, new[] { e1 }, env.Due())).Succeeded);
        Assert.False((await env.Borrowing.CheckoutMultipleAsync(s, new[] { e1, e1 }, env.Due())).Succeeded);
        var other = await env.AddStudentAsync("S-2");
        Assert.True((await env.Borrowing.BorrowAsync(other, e2, env.Due())).Succeeded);
        var mixed = await env.Borrowing.CheckoutMultipleAsync(s, new[] { e1, e2 }, env.Due());
        Assert.False(mixed.Succeeded);

        await using var db = await env.Factory.CreateDbContextAsync();
        Assert.Equal(1, await db.Borrowings.CountAsync());
        Assert.True((await db.Equipment.SingleAsync(x => x.Id == e1)).IsAvailable);
    }

    [SkippableFact]
    public async Task Multi_checkout_is_all_or_nothing_when_a_failure_happens_after_the_first_write()
    {
        await using var env = await TestEnv.CreateAsync();
        var s = await env.AddStudentAsync("S-1");
        var e1 = await env.AddItemAsync("E-1");
        var e2 = await env.AddItemAsync("E-2");

        env.Fault.Enabled = true;
        var r = await env.Borrowing.CheckoutMultipleAsync(s, new[] { e1, e2 }, env.Due());
        env.Fault.Enabled = false;

        Assert.False(r.Succeeded);
        await using var db = await env.Factory.CreateDbContextAsync();
        Assert.Equal(0, await db.Borrowings.CountAsync());
        Assert.All(await db.Equipment.ToListAsync(), x => Assert.True(x.IsAvailable));

        Assert.True((await env.Borrowing.CheckoutMultipleAsync(s, new[] { e1, e2 }, env.Due())).Succeeded);
    }

    [SkippableFact]
    public async Task Database_itself_rejects_a_second_active_borrowing_for_the_same_item()
    {
        await using var env = await TestEnv.CreateAsync();
        var a = await env.AddStudentAsync("S-1");
        var b = await env.AddStudentAsync("S-2");
        var e = await env.AddItemAsync("E-1");

        await using var db = await env.Factory.CreateDbContextAsync();
        db.Borrowings.Add(Borrowing.Issue(a, e, env.Clock.Today, env.Due()));
        await db.SaveChangesAsync();
        db.Borrowings.Add(Borrowing.Issue(b, e, env.Clock.Today, env.Due()));
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        var classifier = new EquipmentBorrowing.Infrastructure.Persistence.NpgsqlErrorClassifier();
        Assert.Equal(DbFailureKind.UniqueViolation, classifier.Classify(ex));
    }

    [SkippableFact]
    public async Task Return_records_the_date_frees_the_item_and_a_repeat_is_rejected()
    {
        await using var env = await TestEnv.CreateAsync();
        var s = await env.AddStudentAsync("S-1");
        var e = await env.AddItemAsync("E-1");
        var borrow = await env.Borrowing.BorrowAsync(s, e, env.Due());
        env.Clock.Today = env.Clock.Today.AddDays(2);

        var ret = await env.Borrowing.ReturnAsync(borrow.Value![0]);
        Assert.True(ret.Succeeded, ret.Message);
        var again = await env.Borrowing.ReturnAsync(borrow.Value![0]);
        Assert.False(again.Succeeded);
        Assert.Equal(ErrorKind.Rejected, again.Kind);

        await using var db = await env.Factory.CreateDbContextAsync();
        var row = await db.Borrowings.SingleAsync();
        Assert.Equal(BorrowingStatus.Returned, row.Status);
        Assert.Equal(env.Clock.Today, row.ReturnedAt);
        Assert.True((await db.Equipment.SingleAsync()).IsAvailable);
    }

    [SkippableFact]
    public async Task History_search_filter_and_sort_work_and_are_stable()
    {
        await using var env = await TestEnv.CreateAsync();
        var s1 = await env.AddStudentAsync("S-1", "Ana Reyes");
        var s2 = await env.AddStudentAsync("S-2", "Ben Cruz");
        var cam = await env.AddItemAsync("CAM-1", "Camera");
        var lap = await env.AddItemAsync("LAP-1", "Laptop");
        var tri = await env.AddItemAsync("TRI-1", "Tripod");

        var b1 = await env.Borrowing.BorrowAsync(s1, cam, env.Due(5));
        var b2 = await env.Borrowing.BorrowAsync(s1, lap, env.Due(3));
        var b3 = await env.Borrowing.BorrowAsync(s2, tri, env.Due(3));
        await env.Borrowing.ReturnAsync(b1.Value![0]);

        var history = (await env.Borrowing.GetStudentHistoryAsync(s1)).Value!;
        Assert.Equal(2, history.Count);
        Assert.All(history, r => Assert.Equal(s1, r.StudentId));

        var active = (await env.Borrowing.GetActiveAsync()).Value!;
        Assert.Equal(2, active.Count);
        Assert.Equal(new[] { b2.Value![0], b3.Value![0] }, active.Select(r => r.Id).ToArray());

        var byText = (await env.Borrowing.SearchAsync(new BorrowingQuery(Text: "cruz"))).Value!;
        Assert.Single(byText);
        var returned = (await env.Borrowing.SearchAsync(new BorrowingQuery(Status: StatusFilter.Returned))).Value!;
        Assert.Single(returned);
        var none = (await env.Borrowing.SearchAsync(new BorrowingQuery(Text: "zzz"))).Value!;
        Assert.Empty(none);

        var gone = await env.Borrowing.GetStudentHistoryAsync(99999);
        Assert.Equal(ErrorKind.NotFound, gone.Kind);
    }

    [SkippableFact]
    public async Task Editing_keeps_history_and_rejects_another_records_identifier()
    {
        await using var env = await TestEnv.CreateAsync();
        var s = await env.AddStudentAsync("S-1", "Ana");
        await env.AddStudentAsync("S-2", "Ben");
        var e = await env.AddItemAsync("E-1", "Cam");
        await env.AddItemAsync("E-2", "Lap");
        var borrow = await env.Borrowing.BorrowAsync(s, e, env.Due());

        Assert.False((await env.Students.UpdateAsync(s, "s-2", "Ana", null)).Succeeded);
        Assert.True((await env.Students.UpdateAsync(s, "S-1", "Ana Maria", "ana@x.edu")).Succeeded);
        Assert.False((await env.Equipment.UpdateAsync(e, "e-2", "Cam", null)).Succeeded);
        Assert.True((await env.Equipment.UpdateAsync(e, "E-1", "Camera Pro", "desc")).Succeeded);

        var hist = (await env.Borrowing.GetStudentHistoryAsync(s)).Value!;
        Assert.Single(hist);
        Assert.Equal("Camera Pro", hist[0].EquipmentName);
        Assert.False((await env.Equipment.SearchAsync(null, true)).Value!.Any(x => x.Id == e));
    }
}
