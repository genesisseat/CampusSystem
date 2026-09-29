# Registrar AI Context

## Ownership

Registrar owns course browsing, registration, academic records, verification, and records-request interfaces in this project. Keep changes local to `RegistrarMain` unless a shared contract or platform change is required.

## How to edit this project

Use the task type to decide the correct layer:

- Front-end / interface work: edit Razor Pages in `Pages/`, the shared layout, and styles. Preserve their live calls to the Registrar API, which reads/writes shared MySQL `mydb` tables.
- Back-end / data work: edit `Controllers/`, `Services/`, `Contracts/`, `Program.cs`, and related API/client files when the task requires course, offering, enrollment, transcript, or document-request behavior. Use shared integer `user.id`; the old EF context and migrations are inactive.
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
- `Services/DatabaseService.cs` = MySQL access used by Registrar Razor Pages
- `Services/RegistrarApiSchemaService.cs` = idempotent `document_requests` bootstrap
- `Controllers/` = MySQL-backed API using shared operational tables

## Interface-only editing guidance

This project has MySQL-backed API routes for course browsing, offerings, enrollment, transcripts, verification, and records requests. Interface-only edits should preserve those API calls and the offering-based enrollment contract.

### Files to edit for UI changes

- Razor Pages in `Pages/`
- Shared layout in `Pages/Shared/_Layout.cshtml`
- Styling in `wwwroot/css/site.css`
- Supporting front-end assets in `wwwroot/`

### What not to add while editing the interface

- Do not wire Razor Page forms directly to the API without an explicit UI task and review.
- Do not move API request validation or claims-derived identity checks into client-side code.
- Do not accept `StudentId` from Razor Page form fields or browser request bodies.
- Do not add student-owned mutations outside the API and shared MySQL tables.

### Approved UI behavior

- Use the Registrar API for live course, enrollment, transcript, verification, and records data.
- Keep claims-derived identity and authorization enforcement on the server; the browser UI must not become the source of truth for student ownership.
- Preserve the existing project structure and keep future UI changes focused on presentation and API integration.

## Department UI

The following Razor Pages are the Registrar UI surface and are wired to the Registrar API for catalog browsing, enrollment, transcript display, verification requests, and records requests:

- `/Courses`: course catalog and browse page
- `/Registration`: registration and schedule builder
- `/Transcript`: semester-grouped transcript view
- `/Verification`: enrollment verification request form
- `/Records`: records request and status tracker

The pages use the existing Bootstrap layout and local styles in `Pages/Shared/_Layout.cshtml` and `wwwroot/css/site.css`.

## API Endpoints

The Registrar API is available for database and authorization testing. All endpoints require authentication; in Development, the `DevelopmentTest` scheme supplies the configured `StudentId` claim and defaults to the `Student` role. Send `X-Test-Role: Admin` only when testing the course seed endpoint.

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/courses?search=` | Browse the course catalog |
| `GET` | `/api/courses/{id}` | Read one course |
| `POST` | `/api/courses` | Create a course; Admin role only |
| `GET` | `/api/registrations/mine` | List the authenticated student's enrollments |
| `POST` | `/api/registrations` | Enroll the authenticated student in a specific offering using `{ offeringId }` |
| `DELETE` | `/api/registrations/{id}` | Drop an owned enrolled subject; seat count is adjusted transactionally |
| `GET` | `/api/transcript/mine` | Read the authenticated student's semester-grouped transcript |
| `GET` | `/api/verifications/mine` | List the student's verification requests |
| `POST` | `/api/verifications` | Create a pending verification request |
| `GET` | `/api/records/mine` | List the student's records requests |
| `POST` | `/api/records` | Create a pending document request |

The Development test identity is configured in `appsettings.Development.json`. Its `StudentId` claim must resolve to a student in the shared MySQL `user` table, by numeric `id` or student number. Test identities must not be accepted from request bodies. Production does not register the Development test authentication scheme.

## Persistence Implementation

- Registrar's Razor pages and API use `DefaultConnection` to shared MySQL `mydb`.
- Catalog and registration use `subjects`, `class_offerings`, `enrollments`, and `enrolled_subjects`.
- Transcripts use the shared `grades` table; records and verification requests use `document_requests`.
- The old EF context and migrations remain as inactive legacy source and are not registered at runtime.

Do not run the legacy Registrar EF migrations. Do not create a second physical database or department-specific connection-string key.

## Change Rules

- Use the shared MySQL `DefaultConnection` and existing operational tables. Direct calls into another department's controllers, services, or API endpoints are not approved.
- Derive `StudentId` from authenticated claims; never accept it from request payloads.
- Protect academic records and enrollment verification with authorization and audit controls as the API expands; the current endpoints enforce authentication and student ownership, while administrative workflows remain deferred.
- Keep student IDs as MySQL integer `user.id`; never create department-local student identities.
- Preserve existing Razor Pages behavior and department namespace.
- Use MySQL/Dapper against existing shared operational tables; do not re-register the legacy SQL Server context or apply its migrations.
- Run `dotnet build RegistrarMain.csproj` after UI changes, API changes, or data changes.
- From the workspace root, run `.\Check-GuidanceServices.ps1 -ProjectPath .\Departments\Registrar\RegistrarMain` for Registrar, or `.\Check-GuidanceServices.ps1 -ProjectPath . -AllDepartments -Build` for the full health check.

##