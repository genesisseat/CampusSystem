# Registrar AI Context

## Ownership

Registrar owns course browsing, registration, academic records, verification, and records-request interfaces in this project. Keep changes local to `RegistrarMain` unless a shared contract or platform change is required.

## How to edit this project

Use the task type to decide the correct layer:

- Front-end / interface work: edit only Razor Pages in `Pages/`, the shared layout in `Pages/Shared/_Layout.cshtml`, and styling in `wwwroot/css/site.css`. Registrar's data lives in its own `RegistrarDbContext`, under the `registrar` SQL schema, within the shared `CampusSystemDb` database. The shared project supplies only the `Student` identity model.
- Back-end / data work: edit `Controllers/`, `Services/`, `Contracts/`, `Models/`, `Data/RegistrarDbContext.cs`, `Migrations/`, `Program.cs`, and related server files only when the task explicitly requires course, enrollment, transcript, or record processing. The shared `Student` identity type lives at `Shared/CampusSystem.Data/Models/Student.cs` and should not be copied into Registrar.
- AI model rule: if the request does not clearly ask for backend logic, assume it is a UI edit and do not add persistence, form submission, or live academic-data processing.

## AI maintenance manual

This file is the project guide for future AI sessions and developer handoffs. It should stay current as the project changes.

### When to update this file

Update this context whenever any of the following changes:

- a new Razor Page, folder, or route is added under `Pages/`
- a new controller, service, contract, model, or DTO is created
- a page starts depending on real registration, transcript, or records data
- department ownership or security responsibilities change
- a page moves from mock UI to real backend behavior

### Required update pattern

When the project structure changes, revise these sections in order:

1. `Ownership` — confirm which team and application area owns the feature
2. `How to edit this project` — confirm whether the work is UI-only or backend
3. `Department UI` — add or remove page names and responsibilities
4. `Files to edit for UI changes` and `What not to add while editing the interface` — keep them aligned with the current files
5. `Change Rules` — add any new auth, validation, or contract requirements

### AI session rule

Before making a change in a future session, read this file first and compare it to the current project structure. If the app has new pages, services, policies, or contract files, update this file to match the real state before continuing the task.

### Project map

- `Pages/` = UI pages and presentation logic
- `Pages/Shared/_Layout.cshtml` = shared shell and global styling entry point
- `wwwroot/css/site.css` = visual theme and component styling
- `Controllers/` = request handling and endpoint behavior
- `Services/` = business logic and integrations
- `Contracts/` = interfaces and shared DTOs
- `Models/` = domain objects and data contracts
- `Data/RegistrarDbContext.cs` = Registrar-owned EF Core context
- `Migrations/` = Registrar-owned migration history for the `registrar` schema
- `Shared/CampusSystem.Data/Models/Student.cs` = shared identity model mapped to `dbo.Students`

## Interface-only editing guidance

This project owns the persistence foundation for course, enrollment, transcript, verification, and records data. `RegistrarDbContext` owns only `registrar.*` tables; it references `dbo.Students` from `CampusSystem.Data` and excludes that table from Registrar migrations. Interface edits should remain presentation-focused unless a task explicitly requests backend wiring.

### Files to edit for UI changes

- Razor Pages in `Pages/`
- Shared layout in `Pages/Shared/_Layout.cshtml`
- Styling in `wwwroot/css/site.css`
- Supporting front-end assets in `wwwroot/`

### What not to add while editing the interface

- No student registration persistence
- No transcript updates or records mutations
- No real verification submission behavior
- No service calls tied to live academic data

### Approved UI behavior

- Use placeholder course and enrollment data to present the user flow.
- Keep schedule-building, transcript, and records actions non-functional until the owning team approves the backend contract and authorization model.
- Preserve the existing project structure and buttons/sections used for presentation only.

## Department UI

The following Razor Pages currently remain presentation placeholders; the database foundation is available, but their controllers/services and form handlers are intentionally deferred:

- `/Courses`: course catalog and browse page
- `/Registration`: registration and schedule builder
- `/Transcript`: semester-grouped transcript view
- `/Verification`: enrollment verification request form
- `/Records`: records request and status tracker

The pages use the existing Bootstrap layout and local styles in `Pages/Shared/_Layout.cshtml` and `wwwroot/css/site.css`.

## Persistence Implementation

- `RegistrarDbContext` is registered in `Program.cs` with the `CampusSystemDb` connection-string key.
- Registrar uses SQL Server LocalDB during development and stores its migration history in `registrar.__EFMigrationsHistory_Registrar`.
- `Course`, `Enrollment`, `TranscriptEntry`, `VerificationRequest`, and `RecordsRequest` are Registrar-owned models mapped to the `registrar` schema.
- Each student-owned record has a foreign key to the shared `CampusSystem.Data.Models.Student` identity model. `dbo.Students` is mapped with `ExcludeFromMigrations()` so Registrar can query it but cannot create, alter, or drop it.
- `Enrollment.RowVersion` is a SQL Server rowversion concurrency token.

From `Departments/Registrar/RegistrarMain`, review and apply Registrar migrations with:

```powershell
dotnet ef migrations add <MigrationName> --context RegistrarDbContext
dotnet ef database update --context RegistrarDbContext
```

Do not add Registrar entities, `DbSet` properties, or migrations to `Shared/CampusSystem.Data`. That shared project contains the identity model only. Do not create a second physical database or a second Registrar connection-string key.

## Change Rules

- Registrar owns `RegistrarDbContext`, its own `Migrations/` folder, and the `registrar` schema inside the shared `CampusSystemDb` database. The Registrar migration history table is `registrar.__EFMigrationsHistory_Registrar`; do not assume there is one shared migration history.
- Registrar does not modify `CampusSystem.Data` except when the shared `Student` model itself needs a field, which requires cross-team coordination.
- Use the shared `CampusSystemDb` connection-string key. Direct calls into another department's controllers, services, or API endpoints are not approved.
- Derive `StudentId` from authenticated claims; never accept it from request payloads.
- Protect academic records and enrollment verification with authorization and audit controls before backend wiring.
- Treat `Enrollment.RowVersion` as a concurrency token and handle update conflicts.
- Keep the shared LocalDB connection string in local configuration or user-secrets outside source control for non-local environments.
- Preserve existing Razor Pages behavior and department namespace.
- Run `dotnet build RegistrarMain.csproj` after UI changes or data changes.
- From the workspace root, run `.\Check-GuidanceServices.ps1 -ProjectPath .\Departments\Registrar\RegistrarMain` for Registrar, or `.\Check-GuidanceServices.ps1 -ProjectPath . -AllDepartments -Build` for the full health check.

##