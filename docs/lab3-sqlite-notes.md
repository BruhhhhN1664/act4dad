# Earlier documentation retained from Lab 3 (SQLite version)

> Short summary of the Lab 3 README kept for the "preserve relevant earlier documentation" requirement.
> Paste the full original text from `https://github.com/BruhhhhN1664/Act3` below this note if your instructor wants it verbatim.

**What Lab 3 did.** Added SQLite persistence (EF Core) to the Campus Equipment Borrowing System built in Labs 1–2:
domain entities `Student`, `Equipment`, `Borrowing`; repository interfaces and EF implementations; an `IUnitOfWork` that committed a borrowing and its
equipment-availability change together; a filtered unique index allowing only one active borrowing per item; a CHECK that the due date is after the borrowed date;
start-up creation of the database file and one-time seed data; setup scripts (`setup.sh`, `setup.ps1`); xUnit persistence tests including “data survives a restart”.

**Lab 3 design choices that Lab 4 changes** (and why):

| Lab 3 | Lab 4 |
|---|---|
| SQLite file database | PostgreSQL via Npgsql (`DbContext` options in `AddPersistence`) |
| Singleton `DbContext` + `SemaphoreSlim` serialising operations | `IDbContextFactory` + one short-lived unit of work per operation (supports overlapping async loads) |
| Borrowed/due/returned as date-times | `DateOnly` calendar dates (BR-11/13) |
| Equipment unique by name; free-text `Type` | Separate normalized unique `EquipmentIdentifier`; optional `Description` |
| Student without contact or normalized number | Optional contact; normalized unique student number |
| Single-item borrow only | UC-06 multi-item atomic checkout with locks and revalidation |
| SQLite migrations | Delete the old `Migrations` folder; new `InitialPostgres` migration |

**Kept unchanged:** the layer names, the one-active-borrowing filtered unique index, the due-after-borrowed CHECK, seed-only-when-empty behaviour, setup scripts, and the use of CommunityToolkit.Mvvm.
