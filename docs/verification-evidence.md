# Verification evidence (Appendix C) — template with the steps to perform

Use a **disposable** database. For every row: run the **Action**, compare with **Expected**, write what you saw under **Observed**, and
attach/point to the **Evidence** (screenshot of the UI *plus* the readback query from `verification-queries.sql` run on a **fresh connection** after the operation; for restart cases close and reopen the app first).
A screenshot alone does not prove persistence or atomicity.

Setup used for all rows: the seeded data (Ana, Ben, Carla [not allowed], Diego, Elena; LAP-01, LAP-02, CAM-01 [borrowed by Ben], PRJ-01 [returned by Ana], TRI-01, MIC-01).
Add `EQUIPMENT_DEMO=1` when you start the app for A06's failure case.

| ID | Action (setup → do) | Expected | Observed | Evidence |
|---|---|---|---|---|
| A01 | Catalog tab. Then: empty DB (truncate in a scratch DB) → empty message. Stop PostgreSQL → press Search | List equals `Equipment` table. Empty state ≠ error. Stopped server → red “Cannot reach the PostgreSQL database…” | | Q1 + screenshots |
| A02 | Tick “only available”; search `zzz`; stop server | CAM-01 excluded; “no equipment matches”; connection error | | Q1 |
| A03 | Issue TRI-01 to Ana (+7 days). Rejections: Carla; item already borrowed (CAM-01); a student with 3 active; date today; date empty. Restart app | Success: 1 new Active row, TRI-01 unavailable (Q2, Q3). Each rejection: message and **no** new rows (compare Q4 counts before/after). Same after restart | | Q2, Q3, Q4 |
| A04 | Active / Return tab; with no active rows; with server stopped | Only Active rows, soonest due first; empty state; error | | Q2 |
| A05 | Return TRI-01; press return again on the stale row; restart | Same row → Returned, today's date, item available, row kept; second attempt “already returned”, no change | | Q3, Q5 |
| A06 | Success: Diego + LAP-01 + LAP-02 → both rows + both items unavailable. Rejections (each leaves counts unchanged): 1 item · same item twice (use the service/tests) · LAP-01 + CAM-01 (unavailable) · date empty/today · Carla · over limit · **exactly at limit** (student with 1 active + 2 items = 3 OK). Failure: start with `EQUIPMENT_DEMO=1`, tick the *simulate failure* box, check out two items | Failure shows “failed part-way … rolled back”; **no** new Borrowings, both items still available (Q4 before/after identical). Document failure point: after the first item was saved inside the open transaction (`BorrowingService`, after first `SaveChangesAsync`) | | Q4 before/after, Q2 |
| A07 | History tab: students with active+returned (Ben/Ana), one with none (Elena); try after deleting nothing (stale) / server stopped | Correct item/date/status per student; empty message vs error message distinct | | Q6 |
| A08 | History tab: two items with history (PRJ-01, CAM-01), one with none (MIC-01) | Correct student/date/status; empty vs error | | Q7 |
| A09 | Search tab: each field (student name, student number, equipment name); each status; both sorts × both directions; combined; Clear; `zzz`; server stopped. Observe the loading bar while it runs | Membership/order equals Q8 results; ties ordered by Id; empty state; error | | Q8 |
| A12 | Register `2024-0100 / Test Student`; again as ` 2024-0100 ` and `2024-0100`; blank number; blank name; restart | One row (normalized unique); duplicates/blank rejected; row survives restart | | Q9, Q4 |
| A13 | Edit Ben (has history): change name, clear contact, set number to Ana's; blank name; restart | Same Id, history intact (Q6), duplicate/blank rejected, no change | | Q9, Q6 |
| A14 | Restrict Ben (active borrower) → try to issue → return CAM-01 → Allow → issue; repeat Restrict twice | Issue rejected; active kept; return works; after Allow issue works; repeat gives “already … no change” | | Q10 |
| A15 | Register `PRJ-02 / Projector 02`; duplicate ` prj-02 `; blank | One available item, no borrowing; duplicates/blank rejected; survives restart | | Q11 |
| A16 | Edit CAM-01 (has history): rename, clear description, identifier of another item; try to change availability (no field exists) | Same Id; history and availability unchanged | | Q6, Q11 |
| A17 | **Clean** database: `DROP DATABASE`, run the app/`dotnet ef database update`; then in a disposable DB run `verification-queries.sql` §C (bad FK, duplicate number, duplicate id, second active borrowing) | Migration output and `__EFMigrationsHistory` row; each bad insert fails with SQLSTATE 23503 / 23505 / 23514 | | Q12, §C |
| A18 | Stop PostgreSQL → attempt a checkout/return → start PostgreSQL → refresh/Retry → repeat. Walk through `BorrowingService.CheckoutCoreAsync`, `ServiceBase`, `EfUnitOfWorkFactory` | Clear error, no false success, no partial rows; works again after restoration. Be ready to explain lifetimes and transaction boundary (`architecture-decisions.md`) | | Q4, code |
| A19 | Follow README on a clean machine/folder; compare diagram/catalog/suite/matrix with the code | Setup works as written; documents match | | – |

## Integrated demonstration script (all nine steps, in order)

1. **Student foundation (A12–A13)** – register a student, find it, edit it. Readback Q9.
2. **Equipment foundation (A15–A16)** – register an item, find it, edit it. Readback Q11.
3. **Cross-workflow borrowing (A03, A12, A15)** – issue the new item to the new student. Q2/Q3.
4. **Eligibility and return (A14, A05)** – restrict the student, show a new request rejected while the existing issue remains, return the item. Q3/Q10.
5. **Coordinated checkout (A06)** – allow the student again and issue two distinct items. Restart, then Q2/Q3.
6. **Invalid and failed checkout (A06)** – request an unavailable item + another item, then the simulated failure. Q4 before/after are identical.
7. **Related histories (A07–A08)** – borrow and return an item; select its student and the item; the same event appears in both histories, unrelated rows excluded.
8. **Retrieval controls (A09)** – search/filter/sort a known mix, reset, no-match.
9. **Restart and recovery (A03, A12–A16, A18)** – restart and retrieve everything; stop/start PostgreSQL to show the error and recovery.

## Automated evidence
`dotnet test` with `EQUIPMENT_TEST_CONNECTION` set (see README §5) covers A03, A05, A06 (incl. rollback), A12–A17 and parts of A07/A09. Paste the test run summary here: ____________
