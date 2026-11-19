# Architecture and design decisions (for the walkthrough, A18)

## Layers and dependencies
`Desktop → Application ← Infrastructure`, both depending on `Domain`. Application defines `IUnitOfWorkFactory`, repositories and
`IDatabaseErrorClassifier`; Infrastructure implements them with EF Core/Npgsql. ViewModels call only service classes.
ViewModels and Views never reference `DbContext`, SQL, `Npgsql`, connection strings or migrations; the connection string is read
once in the composition root (`App.axaml.cs`) and migrations run through `IDatabaseInitializer`.

## DbContext and DI lifetimes
| Object | Lifetime | Why |
|---|---|---|
| `IDbContextFactory<EquipmentBorrowingDbContext>` | singleton (EF default) | creates contexts on demand |
| `EfUnitOfWork` (one DbContext + transaction) | one per service operation | DbContext is not thread-safe; overlapping async loads must not share one |
| Services, error classifier, clock, initializer | singleton | stateless |
| ViewModels, `DemoFaultSwitch` | singleton | tabs keep their UI state; `DemoFaultSwitch` is a test seam |
Lab 3's singleton context and `SemaphoreSlim` serialised everything and would leak tracked entities between operations; the factory removes both problems.

## Transaction boundary and atomic checkout (UC-06)
`BorrowingService.CheckoutCoreAsync` = one transaction: (1) cheap validation, (2) `BEGIN`, (3) `SELECT … FOR UPDATE` the student, (4) lock the items in id order, (5) revalidate eligibility, existence, availability and the active count,
(6) insert one borrowing per item and mark the item unavailable, saving after each item, (7) `COMMIT`. Locking the student serialises two checkouts for the same student so they cannot both pass the limit check; ordering
the item locks avoids deadlocks. Disposing the unit of work without commit rolls back everything, which is what happens for rejections and exceptions.

## Database enforcement (BR-16)
Primary keys, foreign keys (`Restrict` — no cascading deletes), required columns, `UX_Students_StudentNumberNormalized`, `UX_Equipment_IdentifierNormalized`,
**`UX_Borrowings_ActiveByEquipment`** (unique on `EquipmentId` where `ReturnedAt IS NULL`), CHECK `ExpectedReturnAt > BorrowedAt`, CHECK that status and return date agree, CHECK non-blank names/numbers.
Normalisation is `Trim().ToUpperInvariant()` stored in a `…Normalized` column, so the unique index compares exactly what the validation compares.

## Error handling (BR-18/19)
Every service call runs in `ServiceBase.GuardAsync`; provider exceptions are classified by `NpgsqlErrorClassifier` (unavailable / unique / FK / check / other) and turned into messages without credentials or stack traces.
Results are `Result<T>` with an `ErrorKind` so the UI distinguishes **empty** (success with zero rows) from **not found** and **connection error**. A failure inside `CommitAsync` caused by connection loss becomes `UnknownOutcome`:
the message says the result is not known, nothing is retried, and rollback is not claimed.

## Responsiveness
All list loading is `async` with a cancellation token; a newer load cancels an older one and only the newest load clears the busy indicator, so typing in a search box cannot display stale results. Each tab reloads when opened.

## Dates (BR-11/13)
`DateOnly` local calendar dates, from `IClock.Today` (`DateTime.Now`). Expected return must be later than today; return date is today and must not precede checkout.

## Known limits
No hard deletes, maintenance, retirement or overdue views (deferred). Edits are last-write-wins for descriptive fields; identifier collisions are caught by validation and the unique indexes.
