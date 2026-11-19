# One-time setup (Windows PowerShell). Safe to re-run.
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

Write-Host '== 1/4 Checking tools'
dotnet --version
dotnet ef --version *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Installing dotnet-ef (global tool)...'
    dotnet tool install --global dotnet-ef --version '9.*'
    $env:PATH += ";$env:USERPROFILE\.dotnet\tools"
}

Write-Host '== 2/4 Restoring and building'
dotnet build EquipmentBorrowing.slnx

Write-Host '== 3/4 Creating the PostgreSQL migration (only the first time)'
$migDir = 'src/EquipmentBorrowing.Infrastructure/Migrations'
if (Test-Path "$migDir/*.cs") {
    Write-Host "Migrations already exist in $migDir - keeping them."
} else {
    dotnet ef migrations add InitialPostgres `
        --project src/EquipmentBorrowing.Infrastructure `
        --startup-project src/EquipmentBorrowing.Desktop `
        --output-dir Migrations
    Write-Host "Created $migDir  -> commit this folder to Git."
}

Write-Host '== 4/4 Done'
Write-Host @'

Next:
  1. Make sure PostgreSQL is running, then set ConnectionStrings__Postgres to your full connection string for this terminal only:
       $env:ConnectionStrings__Postgres = 'Host=localhost;Port=5432;Database=equipment_borrowing;Username=postgres;Password=<your-password>'
     (or copy src\EquipmentBorrowing.Desktop\appsettings.Local.json.example to src\EquipmentBorrowing.Desktop\appsettings.Local.json, add the password to that ignored local file)
  2. Run the app (it applies the migration, creating the database if needed, and seeds sample data once):
       dotnet run --project src/EquipmentBorrowing.Desktop
  3. Optional automated tests against a DISPOSABLE database (it is dropped and recreated!):
       $env:EQUIPMENT_TEST_CONNECTION = 'Host=localhost;Database=equipment_borrowing_test;Username=postgres;Password=<your-password>'
       dotnet test
'@
