# Campus System AI Context

## Mission

Campus System is a multi-department ASP.NET Core solution. Each department is an independent .NET web project. The current cross-department capability is the Guidance service layer, copied into every department project and registered through dependency injection.

When modifying this workspace, preserve department boundaries, existing Razor Pages behavior, and the service contracts. Prefer small, local changes and validate the affected project with the health checker and `dotnet build`.

## Repository Map

The workspace root is `X:\CampusSystem`.

| Area | Purpose |
| --- | --- |
| `Departments` | Independent department applications |
| `Environment` | Environment and deployment material |
| `Guardrail` | Security or policy guardrails |
| `Requirements` | Product and technical requirements |
| `SQL` | Database scripts |
| `BackupFolder` | Backup material; do not treat as active source |
| `Maintenance` | Local health dashboard, launcher, and AI context |
| `Shared/CampusSystem.Data` | Shared `Student` identity model only — not a shared database layer |
| `Check-GuidanceServices.ps1` | Integrity checker for one project or all departments |
| `Install-GuidanceServices.ps1` | Copies and registers the Guidance service layer |
| `SERVICES.md` | Service behavior and production-readiness notes |

## Departments

Every department currently targets `net10.0`, uses nullable reference types and implicit usings, and has a project folder ending in `Main`.

| Department | Project path | Assembly/namespace |
| --- | --- | --- |
| FacultyPortal | `Departments/FacultyPortal/FacultyPortalMain` | `FacultyPortalMain` |
| Finance | `Departments/Finance/FinanceMain` | `FinanceMain` |
| GuidanceDepartment | `Departments/GuidanceDepartment/GuidanceDepartmentMain` | `GuidanceDepartmentMain` |
| Library | `Departments/Library/LibraryMain` | `LibraryMain` |
| Registrar | `Departments/Registrar/RegistrarMain` | `RegistrarMain` |
| StudentPortal | `Departments/StudentPortal/StudentPortalMain` | `StudentPortalMain` |

GuidanceDepartment is the source/reference implementation for the service layer. The other five projects contain namespace-adjusted copies, so the service layer remains duplicated per project. `Shared/CampusSystem.Data` contains only the shared `Student` identity model. Each department owns its own `DbContext`, its own entity models, and its own migration history inside its own project, scoped to its own SQL schema, all pointed at the one physical `CampusSystemDb` database. No department should need to edit another department's `DbContext` or another department's migrations. The only shared file teams routinely reference is the `Student` model itself.

Cross-department reads may query another department's schema in the same physical database only after that source department has populated it. Department controllers, services, and API endpoints remain separately owned; do not call another department's API surface directly.

## Shared Service Layer

Source location: `Departments/GuidanceDepartment/GuidanceDepartmentMain/Services` and `Contracts`.

- `AuthService`: validates login input, issues short-lived JWT cookies, rotates refresh tokens, and reads `Jwt:SigningKey` from configuration.
- `StudentRequestService`: validates DTOs, enforces student ownership from claims-derived identity, supports idempotent creates, sanitizes safety-valve text, and maps concurrency conflicts.
- `CounselorTriageService`: lists requests, validates status transitions, handles EF concurrency conflicts, and appends audit events.
- `AuditLogService`: append-only in-process audit events with query support. It is scaffolding and is not durable production storage.
- `CsvImportService`: strict CsvHelper roster mapping with validation before commit.
- `PiiMaskingService`: configurable email, phone, and SSN-like redaction.
- `NotificationService`: Polly retry and circuit breaker around an outbound transport; failures are logged, audited, and return `false`.
- `InMemoryGuidanceRequestStore`: development-only request store.
- `InMemoryRefreshTokenStore`: development-only refresh token store.
- `UnavailableOutboundMessageTransport`: development fallback that deliberately fails until a real transport is configured.

Important contracts are in `Contracts/ServiceContracts.cs`. Controllers should obtain student identity from claims, never from request payloads. Service methods return DTOs and `ServiceResult<T>` values where applicable.

## Dependency Registration

Registrations live in each department's `Program.cs` and should remain consistent with the source project:

- Stores and audit service: singleton in-memory registrations.
- Auth, request, triage, CSV, transport, and notification services: scoped.
- PII masking service: singleton.
- FluentValidation assembly registration: `AddValidatorsFromAssemblyContaining<StudentRequestValidator>()`.

When adding a service, update the interface, implementation, registration, focused tests, package references, and this context if the architecture changes.

## Maintenance Tools

### Integrity checker

Run one project from its project directory:

```powershell
.\Check-GuidanceServices.ps1 -ProjectPath .
```

Run all departments from the workspace root:

```powershell
.\Check-GuidanceServices.ps1 -ProjectPath . -AllDepartments
```

Include compilation:

```powershell
.\Check-GuidanceServices.ps1 -ProjectPath . -AllDepartments -Build
```

The checker verifies contract and service files, namespace imports, DI markers, NuGet references, and optionally builds. Exit code `0` means all checks pass; exit code `1` means one or more checks failed.

### Installer/repair script

The installer targets FacultyPortal, Finance, Library, Registrar, and StudentPortal:

```powershell
.\Install-GuidanceServices.ps1 -WhatIf
.\Install-GuidanceServices.ps1
```

Repair one target:

```powershell
.\Install-GuidanceServices.ps1 -Department Finance
```

Backups are written under each target project's `.guidance-services-backup`. Review backups before removing them. GuidanceDepartment is the source and is not an installer target.

### Maintenance dashboard

Files in `Maintenance`:

- `index.html`: browser dashboard with Campus and department terminal tabs, department cards, green/red process indicators, clickable department web-address links, Start/Stop buttons, bulk Start all/Stop all controls, error list, and terminal-style output. Process states and the selected department log refresh every `500 ms`; full builds run only when requested.
- `Start-MaintenanceDashboard.ps1`: local-only `HttpListener` host with health and department lifecycle APIs. Failed department processes with non-zero exit codes are removed from the running set and remain stopped. Process status includes `Running`, `Managed`, `CanStop`, and `Error` so the UI can distinguish Maintenance-owned processes from externally launched processes.
- `Run-MaintenanceMonitoring.bat`: starts the host on port `5080` and opens the browser.
- `AI-CONTEXT_DepartmentMaintainance .md`: this document.

Start it with:

```powershell
cd X:\CampusSystem
.\Maintenance\Start-MaintenanceDashboard.ps1
```

Open `http://localhost:5080/`, then select **Run full debug**. The dashboard runs the checker for all departments with `-Build` and displays failures in the terminal panel. Green dots mean a department process is running; red dots mean it is stopped. Each card can start or stop its department with `dotnet run`, and a running Maintenance-owned process has an **Open site** link for its assigned localhost address. Logs are written to `Maintenance/logs`. Maintenance-owned processes can be stopped from the dashboard; processes launched by Visual Studio are displayed as running but are not stopped by Maintenance.

The dashboard API is:

- `GET /api/health?build=true`: run integrity and build checks for all departments.
- `GET /api/departments`: return current process states.
- `POST /api/departments/start-all`: start every department process.
- `POST /api/departments/stop-all`: stop every department process.
- `POST /api/departments/{name}/start`: start one department project.
- `POST /api/departments/{name}/stop`: stop one department process tree.
- `GET /api/departments/{name}/log`: return the latest output and error log for a department terminal tab.

Maintenance assigns distinct high localhost ports to department processes started from the dashboard:

| Department project | Maintenance port |
| --- | ---: |
| `FacultyPortalMain` | `52141` |
| `FinanceMain` | `52142` |
| `GuidanceDepartmentMain` | `52143` |
| `LibraryMain` | `52144` |
| `RegistrarMain` | `52145` |
| `StudentPortalMain` | `52146` |

These ports are passed through `dotnet run --urls` and are intended to reduce collisions with Visual Studio launch ports. They are not a security boundary and may still be changed if another process already uses one.

The **Open site** links use the assigned Maintenance URL for Maintenance-owned processes. For processes launched externally, including Visual Studio, the host checks the process's listening localhost port and uses that actual port when available. External processes are reported as running but their Maintenance Stop button remains disabled; stop them from Visual Studio or terminate their process tree explicitly before starting the same project through Maintenance.

When a department process exits with a non-zero exit code, the host treats it as an error, removes it from the active process set, and reports it as stopped. Do not restart it automatically until the underlying error is investigated.

If Visual Studio is already running a department, do not start that same project through Maintenance. Visual Studio owns the generated executable in `bin/Debug/net10.0`, and a Maintenance build can fail with `MSB3027` or `MSB3021` because the executable is locked. Use the Visual Studio process, or stop it in Visual Studio before starting it from Maintenance. A typical lock message names the process and ends with `The file is locked by: "DepartmentMain (PID)"`.

The host handles an occupied port. If the requested port already serves this dashboard, it reports the existing URL. If another process owns it, start on another port, for example:

```powershell
.\Maintenance\Start-MaintenanceDashboard.ps1 -Port 5081
```

Stop the host with `Ctrl+C`. Stopping the host also stops department processes started by that host.

The dashboard is intentionally local-only. Keep it bound to `localhost`; do not expose it to a network without authentication and authorization. The `Campus` terminal tab shows dashboard/debug activity; each department tab shows that project's latest `dotnet run` output and error log. If a department is already running from Visual Studio, the dashboard reports it as running, does not rebuild it, and disables its Maintenance Stop button; stop that process from Visual Studio before using Maintenance for that project.

The default dashboard port is `5080`. If an older dashboard listener remains on that port, start the corrected host on another local port, such as `5081`, and open that URL instead.

## Security and Production Boundaries

- Keep JWT signing keys and connection strings out of JSON and source control. Use `dotnet user-secrets` locally and a managed secret provider such as Azure Key Vault in production.
- Replace in-memory stores with EF Core/Identity-backed implementations before production.
- Replace `UnavailableOutboundMessageTransport` with an approved email, SMS, or messaging transport.
- Make audit storage durable and access-controlled before production.
- Keep rate limiting, HTTPS, authentication, authorization, PII masking, and audit behavior enabled where applicable.
- Run the dependency vulnerability check before merging package changes:

```powershell
dotnet list package --vulnerable
```

## AI Change Rules

1. Identify the owning department project and direct implementation before editing.
2. Preserve the target namespace for that department when copying or changing service files.
3. Do not edit generated `bin`, `obj`, or `.vs` output.
4. Do not overwrite user changes or backups without inspection.
5. After edits, run the narrowest relevant checker first, then build the affected project.
6. If changing checker output labels or dashboard API shape, update both the parser and the HTML consumer.
7. Treat a passing integrity check as structural health only; it does not prove production persistence, external messaging, or real authentication configuration.

## Current Validation Expectations

A complete workspace validation should report six department projects. A healthy result requires all expected service files, contracts, namespaces, DI registrations, package references, and builds to pass. Build failures should be reported with the department name and compiler diagnostic, not hidden behind a generic health status.