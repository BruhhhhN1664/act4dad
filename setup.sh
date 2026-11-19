#!/usr/bin/env bash
# One-time setup (macOS / Linux / Git Bash). Safe to re-run.
set -euo pipefail
cd "$(dirname "$0")"

echo "== 1/4 Checking tools"
dotnet --version
if ! dotnet ef --version >/dev/null 2>&1; then
  echo "Installing dotnet-ef (global tool)..."
  dotnet tool install --global dotnet-ef --version "9.*"
  export PATH="$PATH:$HOME/.dotnet/tools"
fi

echo "== 2/4 Restoring and building"
dotnet build EquipmentBorrowing.slnx

echo "== 3/4 Creating the PostgreSQL migration (only the first time)"
MIGDIR="src/EquipmentBorrowing.Infrastructure/Migrations"
if ls "$MIGDIR"/*.cs >/dev/null 2>&1; then
  echo "Migrations already exist in $MIGDIR - keeping them."
else
  dotnet ef migrations add InitialPostgres \
    --project src/EquipmentBorrowing.Infrastructure \
    --startup-project src/EquipmentBorrowing.Desktop \
    --output-dir Migrations
  echo "Created $MIGDIR  -> commit this folder to Git."
fi

echo "== 4/4 Done"
cat <<'MSG'

Next:
  1. Make sure PostgreSQL is running and set ConnectionStrings__Postgres to your full connection string for this terminal only:
       export ConnectionStrings__Postgres='Host=localhost;Port=5432;Database=equipment_borrowing;Username=postgres;Password=<your-password>'
     (or copy src/EquipmentBorrowing.Desktop/appsettings.Local.json.example to src/EquipmentBorrowing.Desktop/appsettings.Local.json and add the password to that ignored local file)
  2. Run the app. It applies the migration (creating the database if needed) and seeds sample data once:
       dotnet run --project src/EquipmentBorrowing.Desktop
     or apply the schema yourself first:
       dotnet ef database update --project src/EquipmentBorrowing.Infrastructure --startup-project src/EquipmentBorrowing.Desktop
  3. Optional automated tests against a DISPOSABLE database (it is dropped and recreated!):
       export EQUIPMENT_TEST_CONNECTION='Host=localhost;Database=equipment_borrowing_test;Username=postgres;Password=<your-password>'
       dotnet test
MSG
