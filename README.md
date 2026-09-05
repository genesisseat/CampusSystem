# CampusSystem

CampusSystem is a multi-department ASP.NET Core solution for campus operations. Each department is an independent web application with its own UI, services, database context, SQL schema, and migration history.

The applications share one physical SQL Server LocalDB database during development, but they do not share one database context. This keeps department ownership clear while allowing approved cross-department reads through qualified database schemas.

## Applications

| Department | Project | Primary responsibility |
| --- | --- | --- |
| Faculty Portal | `Departments/FacultyPortal/FacultyPortalMain` | Faculty rosters, gradebook, attendance, feedback, and schedules |
| Finance | `Departments/Finance/FinanceMain` | Billing, invoices, payments, and financial aid |
| Guidance Department | `Departments/GuidanceDepartment/GuidanceDepartmentMain` | Student requests, counselor triage, appointments, and resources |
| Library | `Departments/Library/LibraryMain` | Catalog, circulation, history, fines, and reservations |
| Registrar | `Departments/Registrar/RegistrarMain` | Courses, registration, academic records, verification, and records requests |
| Student Portal | `Departments/StudentPortal/StudentPortalMain` | Student-facing schedules, grades, enrollment, finances, announcements, and requests |

All department applications target `net10.0` and use nullable reference types and implicit usings.

## Architecture

### Department ownership

Each department owns:

- Its ASP.NET Core application and UI.
- Its controllers, services, contracts, and department-specific models.
- Its EF Core `DbContext`.
- Its SQL schema in `CampusSystemDb`.
- Its migration files and migration-history table.

The department boundary does not permit direct calls to another department's controllers, services, or API endpoints.

### Shared identity library

`Shared/CampusSystem.Data` contains only the shared `Student` identity model. It is not a shared database layer and does not contain department entities or a shared `DbContext`.

The shared `Student` model maps to `dbo.Students`. Department contexts may reference it for relationships, but a department context must use `ExcludeFromMigrations()` for `dbo.Students` so it cannot create, alter, or drop the identity table.

### Database

Development uses one physical database:

```text
Server=(localdb)\\mssqllocaldb;Database=CampusSystemDb;Trusted_Connection=True;TrustServerCertificate=True
```

The connection-string key is `CampusSystemDb`. Keep credentials and non-local connection strings out of source control; use user-secrets or a managed secret provider for shared, staging, and production environments.

Schemas and migrations are owned independently. Registrar currently uses:

- Schema: `registrar`
- Context: `RegistrarDbContext`
- History table: `registrar.__EFMigrationsHistory_Registrar`
- Identity table referenced but not owned: `dbo.Students`

Future departments should follow the same pattern with their own context, schema, and history table.

## Prerequisites

- Windows with PowerShell.
- .NET SDK 10.
- SQL Server Express LocalDB.
- `dotnet-ef` version matching the EF Core packages, for example:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.11
```

Check LocalDB:

```powershell
sqllocaldb info
```

## Build

Build an individual project from its project directory:

```powershell
cd Departments\Registrar\RegistrarMain
dotnet build RegistrarMain.csproj
```

Build all department applications:

```powershell
Get-ChildItem .\Departments -Recurse -Filter *.csproj |
    Where-Object { $_.Name -notlike '*.Tests.csproj' } |
    ForEach-Object { dotnet build $_.FullName }
```

Build the shared identity library:

```powershell
cd Shared\CampusSystem.Data
dotnet build CampusSystem.Data.csproj
```

## Database migrations

Run department migrations from that department's project directory. Registrar example:

```powershell
cd Departments\Registrar\RegistrarMain
dotnet ef migrations add <MigrationName> --context RegistrarDbContext
dotnet ef database update --context RegistrarDbContext
```

Review generated migrations before applying them. Do not put department entities or migration files in `Shared/CampusSystem.Data`.

## Health checks

Check Registrar only:

```powershell
.\Check-GuidanceServices.ps1 -ProjectPath .\Departments\Registrar\RegistrarMain
```

Check all departments:

```powershell
.\Check-GuidanceServices.ps1 -ProjectPath . -AllDepartments
```

Run checks and builds:

```powershell
.\Check-GuidanceServices.ps1 -ProjectPath . -AllDepartments -Build
```

A successful full check reports six healthy projects. The checker validates expected service files, namespaces, dependency-injection registrations, package references, and optionally compilation.

## Maintenance dashboard

The local maintenance dashboard monitors department processes and health checks.

Start it from the workspace root:

```powershell
.\Maintenance\Start-MaintenanceDashboard.ps1
```

Then open `http://localhost:5080/`.

The dashboard is local-only. Do not expose it to a network without adding authentication and authorization.

## Repository map

- [CampusSystem project map](CampusSystem-Project-Map.md)
- [Maintenance AI context](Maintenance/AI-CONTEXT_DepartmentMaintainance%20.md)
- [Build and structure instructions](BuildStruct)
- [Requirements](Requirements)
- [SQL scripts and configuration](SQL)
- [Shared identity library](Shared/CampusSystem.Data)
- [Maintenance dashboard](Maintenance)

Each department also has its own `Ai-context_*.md` file. Read the owning department's context before changing its code.

## Development rules

- Keep changes inside the owning department unless a shared identity change is required.
- Do not create a second physical database for a department.
- Do not add department `DbSet` properties to `CampusSystem.Data`.
- Do not call another department's controllers, services, or API endpoints directly.
- Derive student identity from authenticated claims for backend operations; do not trust an authoritative student ID from request payloads.
- Add authorization, validation, audit behavior, and concurrency handling before enabling real academic, financial, library, or guidance workflows.
- Do not edit generated `bin`, `obj`, or `.vs` output.

## Current implementation status

Registrar has the first department-owned persistence implementation. Its UI routes remain presentation-oriented, while the database models, context, schema, and migrations are in place for deliberate backend wiring.

The other department applications retain their placeholder UI workflows and reference the shared identity library, but their department-specific persistence is intentionally deferred.
