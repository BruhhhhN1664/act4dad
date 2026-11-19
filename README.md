# act4dad

## Campus Equipment Borrowing System — Laboratory Activity 4 (PostgreSQL)

ITSD 81 · Avalonia + CommunityToolkit.Mvvm · EF Core 9 + **PostgreSQL (Npgsql)** · .NET 9

Continuation of the Lab 1–3 project. Lab 4 moves the database from SQLite to PostgreSQL, keeps UC-01–05 working,
and adds multi-item checkout, student/equipment management, histories and search (UC-06–09, UC-12–16).
Earlier (Lab 3) documentation is kept in [`docs/lab3-sqlite-notes.md`](docs/lab3-sqlite-notes.md).

> **Partners / student IDs:** _fill in full names and student IDs here and in your submission comment._

## 1. Prerequisites

| Tool | Notes |
|---|---|
| .NET SDK 9 (or newer) | `dotnet --version`. `.slnx` needs SDK 9.0.200+. If you use another TFM, change it once in `Directory.Build.props`. |
| PostgreSQL 15+ | Local server, default port 5432. Windows/macOS: installer from postgresql.org (EDB) or Homebrew `brew install postgresql@17`; Debian/Ubuntu: `sudo apt install postgresql`. |
| `dotnet-ef` tool | Installed automatically by the setup script (`dotnet tool install --global dotnet-ef`). |
| Git | Optional but required for the submission history. |

No Docker, cloud or paid service is used.

## 2. Secret-free connection configuration

The repository contains **no password**. `appsettings.json` holds only host/port/database/user:

```
Host=localhost;Port=5432;Database=equipment_borrowing;Username=postgres
```

Provide the password locally, without committing it:

1. Set `ConnectionStrings__Postgres` to the full connection string in your shell:
   - macOS/Linux: `export ConnectionStrings__Postgres='Host=localhost;Port=5432;Database=equipment_borrowing;Username=postgres;Password=<your-password>'`
   - PowerShell: `$env:ConnectionStrings__Postgres = 'Host=localhost;Port=5432;Database=equipment_borrowing;Username=postgres;Password=<your-password>'`
2. Or copy `src/EquipmentBorrowing.Desktop/appsettings.Local.json.example` to `src/EquipmentBorrowing.Desktop/appsettings.Local.json` and add the password to that local copy. The real file is git-ignored.

The database `equipment_borrowing` is created automatically by the first migration run if your PostgreSQL user may create databases.

## 3. Setup, migrations and initial data

```bash
./setup.sh          # macOS / Linux / Git Bash        (Windows: .\setup.ps1)
```

The script builds the solution and creates the EF Core migration `InitialPostgres` the **first time** only
(`src/EquipmentBorrowing.Infrastructure/Migrations`). **Commit that folder** — it is the reproducible schema.

> If you copy these files over your Lab 3 repository, **delete the old SQLite `Migrations` folder first**, then run the script.

Apply the schema in either way:

* just start the app — it runs `Database.MigrateAsync()` once at startup (existing data is kept), or
* `dotnet ef database update --project src/EquipmentBorrowing.Infrastructure --startup-project src/EquipmentBorrowing.Desktop`

**Initial data** (`SeedData`) is inserted only when *both* the Students and Equipment tables are empty, so restarts never
overwrite or duplicate data. It creates 5 students (one not allowed to borrow), 6 equipment items, one active borrowing
(Camera 01 → Ben Cruz) and one returned borrowing (Projector 01 → Ana Reyes).

To start again from scratch (disposable DB only): `DROP DATABASE equipment_borrowing;` then run the app.

## 4. Run

```bash
dotnet run --project src/EquipmentBorrowing.Desktop
```

If PostgreSQL is not reachable, a banner explains it and offers **Retry**; the app never crashes on a connection failure.

Optional demo switch for the rollback demonstration: start with `EQUIPMENT_DEMO=1` to show the
“simulate a failure after the first write” checkbox on the *Issue equipment* tab.

## 5. Automated tests

Integration tests run against a **real, disposable** PostgreSQL database (it is dropped and recreated per test):

```bash
export EQUIPMENT_TEST_CONNECTION='Host=localhost;Database=equipment_borrowing_test;Username=postgres;Password=<your-password>'
dotnet test
```

Without that variable the tests are reported as *skipped*, not passed.

## 6. Screens ↔ use cases

| Tab | Use cases |
|---|---|
| Catalog | UC-01, UC-02 |
| Issue equipment | UC-03 (one item), UC-06 (several items, one transaction) |
| Active / Return | UC-04, UC-05 |
| Students | UC-12, UC-13, UC-14 |
| Equipment | UC-15, UC-16 |
| History | UC-07, UC-08 |
| Search borrowings | UC-09 |

## 7. Architecture in short

```
Avalonia View → ViewModel → Application Service → Repository abstraction → EF Core (Infrastructure) → PostgreSQL
```

* **Domain** – `Student`, `Equipment`, `Borrowing`, normalization rules (`TextRules`), the limit of 3.
* **Application** – services (`StudentService`, `EquipmentService`, `BorrowingService`), `Result<T>`, repository/unit-of-work interfaces. No EF, no Npgsql.
* **Infrastructure** – DbContext, entity configurations (keys, FKs, unique + filtered indexes, CHECK constraints), repositories, `Npgsql` error classifier, migrations, seeding.
* **Desktop** – Views (XAML), ViewModels (CommunityToolkit.Mvvm), composition root (`App.axaml.cs`). Views/ViewModels contain no SQL, no connection configuration and no migrations.

**Context lifetime.** The Lab 3 singleton `DbContext` + `SemaphoreSlim` cannot serve overlapping async loads (two tabs loading, search while typing). Lab 4 registers an
`IDbContextFactory<T>`; every service operation opens one short-lived *unit of work* (one DbContext, optionally one transaction) and disposes it. Services and ViewModels are singletons because they hold no database state.

**Transaction boundary.** One service call = one transaction (`BorrowingService.CheckoutCoreAsync`). Inside it, current data is **re-read and re-validated**
(student eligibility, item existence/availability, active count) after taking `SELECT … FOR UPDATE` row locks (student first, then items in id order). Each item is saved
inside the open transaction and the transaction is committed once; any exception disposes it → rollback. PostgreSQL also enforces the rules
(unique filtered index = one active borrowing per item), so even a race that slipped past the application check fails safely.

**Unknown commit outcome (BR-18).** If the connection is lost *during commit* the service throws `CommitOutcomeUnknownException`, which becomes an `UnknownOutcome` result:
the UI says the outcome is not known, does not retry, does not claim rollback, and asks staff to refresh and check first.

**Dates.** Checkout/expected/return dates are `DateOnly` local calendar dates (`date` columns), taken from `IClock` (`DateTime.Now` local date).

More detail: [`docs/architecture-decisions.md`](docs/architecture-decisions.md).

## 8. Documentation and evidence

| File | Content |
|---|---|
| `docs/use-case-diagram.md` | Updated use-case diagram (Custodian as primary actor) |
| `docs/use-case-catalog.md` | Catalog with Lab 4 classification and where each case is implemented |
| `docs/use-case-suite.md` | The fourteen required use-case tables |
| `docs/traceability-matrix.md` | Use case → rules → code → tests → evidence |
| `docs/verification-evidence.md` | A01–A19 expected/observed/evidence template + the 9-step integrated demo script |
| `docs/verification-queries.sql` | Fresh-connection readbacks and constraint attempts for a disposable DB |
| `docs/database-schema.md` | ER diagram and constraint list |
| `docs/git-history.md` | Suggested commit sequence and how to export history |

## 9. Troubleshooting

| Symptom | Fix |
|---|---|
| Banner: “Cannot reach the PostgreSQL database” | Start the server; check host/port and the `ConnectionStrings__Postgres` environment variable or `appsettings.Local.json`; press **Retry**. |
| “No EF Core migrations were found” | Run `setup.sh` / `setup.ps1` once (creates `InitialPostgres`). |
| `password authentication failed` | Wrong or missing password; see §2. |
| `permission denied to create database` | Create it once: `createdb -U postgres equipment_borrowing`. |
| Error mentioning an old SQLite migration | Delete the old `Migrations` folder, run the setup script again. |

## 10. Build notes

This solution was written without access to a .NET SDK, so **do your first `dotnet build` early** and fix anything the compiler reports
(package versions are pinned in each `.csproj`; newer patch versions are fine). If your Lab 3 project used a different target framework or Avalonia version,
keep yours: change `Directory.Build.props` and the `Avalonia*` package versions together so they stay on the same version.
