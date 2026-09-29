# CampusSystem

CampusSystem is a multi-department ASP.NET Core solution for campus operations. Each department is an independent web application, and the operational portals share one MySQL database.

The shared operational database is MySQL `mydb`. Departments use the same `DefaultConnection` setting and shared student key (`user.id`); do not introduce department-specific databases or duplicate student identities.

## Applications

| Department | Project | Primary responsibility |
| --- | --- | --- |
| Faculty Portal | `Departments/FacultyPortal/FacultyPortalMain` | Faculty rosters, gradebook, attendance, feedback, and schedules |
| Finance | `Departments/Finance/FinanceMain` | Billing, invoices, payments, and financial aid |
| Guidance Department | `Departments/GuidanceDepartment/GuidanceDepartmentMain` | Student requests, counselor triage, appointments, and resources |
| Library | `Departments/Library/LibraryMain` | Catalog, circulation, history, fines, and reservations |
| Registrar | `Departments/Registrar/RegistrarMain` | Courses, registration, academic records, verification, and records requests |
| Student Portal | `Departments/StudentPortal/StudentPortalMain` | Student-facing schedules, grades, enrollment, finances, announcements, and requests |
| Testing | `Departments/Testing/TestingMain` | QA workflows, regression tracking, test-case management, and bug triage |

All department applications target `net10.0` and use nullable reference types and implicit usings.

## Architecture

### Department ownership

Each department owns:

- Its ASP.NET Core application and UI.
- Its controllers, services, contracts, and department-specific models.
- Its feature logic and access to the shared operational tables.
- Any department-specific tables it needs, defined compatibly in the shared `mydb` database.

The department boundary does not permit direct calls to another department's controllers, services, or API endpoints.

### Shared identity library

`Shared/CampusSystem.Data` contains a legacy shared `Student` model, not a shared database context. Active cross-department records use MySQL `user.id` (integer) from `mydb`; do not assume the legacy GUID model is the operational identity source.

### Database

All departments use this single MySQL database:

```text
Server=<mysql-host>;Port=3306;Database=mydb;Uid=<mysql-user>;Pwd=<secret>;
```

The configuration key is `ConnectionStrings:DefaultConnection`. Provide it as the `ConnectionStrings__DefaultConnection` environment variable or through a secret manager. Connection credentials must not be committed to source control.

Existing workflows in Finance, Faculty, Registrar, Student Portal, Guidance, and Library now target `mydb`. Registrar's course, enrollment, transcript, and student-request API uses the same operational tables as its Razor pages; its old SQL Server EF context and migrations are inactive legacy files and must not be applied.

Several active pathways already share MySQL records: Faculty reads rosters and writes grades; Student Portal reads and writes operational tables; Guidance persists requests; Finance writes payment/clearance records; and Registrar reads/writes courses, offerings, enrollments, transcripts, and document requests. Feature coverage still varies by department, and some screens/services remain backed by mocks or placeholders.

## Prerequisites

- Windows with PowerShell.
- .NET SDK 10.
- Network access to the shared MySQL host and an externally supplied `ConnectionStrings__DefaultConnection`.

## Build

Build an individual project from its project directory:

```powershell
cd Departments\Registrar\RegistrarMain
dotnet build RegistrarMain.csproj
```

Build and run the Testing department site:

```powershell
cd .\Departments\Testing\TestingMain
dotnet build TestingMain.csproj
dotnet run --project TestingMain.csproj
```

Then open the local app in a browser:

```text
https://localhost:5001/
```

If the dev certificate is not yet trusted, run:

```powershell
dotnet dev-certs https --trust
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

## Database changes

Coordinate changes to shared tables across departments. The Registrar app only bootstraps `document_requests`; it does not create or migrate the shared student, subject, enrollment, offering, or grade tables.

Do not apply the inactive Registrar SQL Server EF migrations. Keep schema changes idempotent and compatible with existing MySQL records.

## Health checks

Check Registrar only:

```powershell
.\Check-GuidanceServices.ps1 -ProjectPath .\Departments\Registrar\RegistrarMain
```

Check all departments for MySQL wiring and builds:

```powershell
.\Check-GuidanceServices.ps1 -ProjectPath . -AllDepartments
```

Run checks and builds:

```powershell
.\Check-GuidanceServices.ps1 -ProjectPath . -AllDepartments -Build
```

The checker validates all seven department projects for MySQL wiring, credential hygiene, obsolete SQL Server startup configuration, and optionally compilation. Add `-RequireDatabaseConfiguration` to require `ConnectionStrings__DefaultConnection` in the current process environment.

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

Shared MySQL connectivity is established for all departments. It does not imply full workflow synchronization: Finance still has in-memory assessment data, Library circulation remains a placeholder, and some student identity/session behavior remains hard-coded. See [the shared database handoff](Departments/SHARED-MYSQL-DATABASE.md) and [the issue tracker](AI_CONTEXT_SYSTEM_ISSUES.md) for current boundaries.
