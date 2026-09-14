# GuidanceDepartment AI Context


## Ownership


GuidanceDepartment owns the student-support and counselor workflow interfaces in this project. This project uses static HTML under `wwwroot` alongside its API and controllers.


## How to edit this project


Use the task type to decide the correct layer:


- Front-end / interface work: edit static pages in `wwwroot/`, shared styling in `wwwroot/styles.css`, and navigation/layout structure in the HTML files. Keep forms and workflow cards visual placeholders unless an approved contract exists.
- Back-end / data work: edit `Controllers/`, `Services/`, `Contracts/`, DTOs, models, `Program.cs`, and other server files only when the task explicitly requires guidance data, case note logic, or a live API contract.
- AI model rule: if the request does not clearly include backend requirements, assume it is an interface edit and do not add persistence, API calls, or live student-record submission.
- Department DB wiring: if a task requires the Guidance UI or API to read live data, connect this project to the shared `CampusSystemDb` using the department `DbContext`, not a standalone Guidance database.


Update this context whenever any of the following changes:


- a new static page, route, or folder is added under `wwwroot/`
- a new controller, service, contract, model, or DTO is created
- a page starts depending on real guidance data, case notes, or counselor workflows
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


- `wwwroot/` = static front-end pages and assets
- `wwwroot/styles.css` = visual theme and front-end styling
- `Controllers/` = request handling and API endpoints
- `Services/` = business logic and guidance integrations
- `Contracts/` = interfaces and shared DTOs
- `Models/` = domain objects and data contracts


## Interface-only editing guidance


This project includes a UI layer and a backend API surface, but the static interface pages are intentionally presentation-only unless the owning team has approved the contract. When editing the interface, keep the work in the front-end layer.


### Files to edit for UI changes


- Static pages in `wwwroot/`
- Shared styling in `wwwroot/styles.css`
- Navigation and page structure in the static HTML files
- App shell and page flow in `wwwroot/index.html`


### What not to add while editing the interface


- No database writes or persistence
- No service calls that create or update real guidance records
- No form submissions to live endpoints without an approved contract
- No bypass of the existing authorization boundaries for case notes or counselor workflows
- Do not create a separate Guidance database or add Guidance tables into `CampusSystem.Data`; this project uses the shared campus database (`CampusSystemDb`) through the central connector in `CampusSystem/SQL/CampusSystemDbConnector.cs` and the appsettings `ConnectionStrings:CampusSystemDb` value.
- Direct calls into another department's controllers, services, or API endpoints remain unapproved.


### Approved UI behavior


- Keep forms, cards, and triage lanes as visual placeholders until the backend contract is approved.
- Preserve the static-file structure and security boundaries around sensitive student data.
- Use the existing `StudentRequestService` DTO shape only as a design reference; do not connect the form to real storage without approval.


## Department UI


Guidance currently uses the shared campus SQL connector and central campus database. The front-end pages remain presentation-only, but the project is now wired to the shared `CampusSystemDb` connection and should keep department business logic and student data access behind the Guidance service layer and its authorization boundaries.

### How to connect Guidance to the shared campus database

When a new Guidance data task needs live records, use this pattern:

1. Read the campus connection string named `CampusSystemDb` from `appsettings.json`.
2. Resolve it in `Program.cs` and pass it to the Guidance-specific `DbContext`.
3. Register the context with `AddDbContextFactory<GuidanceDbContext>` or `AddDbContext<GuidanceDbContext>` using `UseSqlServer(campusConnection)`.
4. Keep the `GuidanceDbContext` and its model mappings in this project, while the shared `Student` identity model stays in `CampusSystem.Data`.
5. Query only the `CampusSystemDb` physical database; do not create a second Guidance database or a separate campus database file.
6. Use the `Students` table and the shared campus schema for cross-department reads when the record is already created by the owning department.

Example:

```csharp
var campusConnection = builder.Configuration.GetConnectionString("CampusSystemDb");

builder.Services.AddDbContextFactory<GuidanceDbContext>(options =>
    options.UseSqlServer(campusConnection));
```

This keeps the project aligned with the rest of the campus system while preserving department ownership. The Guidance pages can remain UI-first, but any live student data must be served through this shared-DB pattern.

- `/appointments.html`: appointment calendar and picker
- `/case-notes.html`: restricted-access case notes panel
- `/resources.html`: college and career resources list
- `/dashboard.html`: counselor dashboard with local fixture data
- `/monitoring.html`: counselor-only student follow-up register with local fixture data and filters
- `/admin.html`: department routing and escalation settings preview


The pages reuse `wwwroot/styles.css` and are linked from the Guidance home page. `wwwroot/prototype.js` supplies local fixture data and preview-only interactions for the dashboard, student monitoring register, triage queue, request form, case notes, and appointment slots. The monitoring page shows masked student references, programme, support signal, request, follow-up date, and assigned counselor; it is not a student-record system. The request form should remain aligned with the `StudentRequestService` DTO shape, but it must not submit until the owning team connects it to the approved contract. Case notes require real authorization and restricted storage before use. The dashboard, monitoring, and admin pages are visual prototypes only and must not be treated as department-scoped authorization or configuration.


## Change Rules


- Preserve the static-file/controller architecture; do not convert to Razor Pages without an explicit decision.
- Do not add persistence or service calls to placeholder pages without an approved contract.
- Use the shared campus database connection rather than creating a separate Guidance database. The current connection is centralized in `CampusSystem/SQL/CampusSystemDbConnector.cs` and reads the `CampusSystemDb` connection string from `appsettings.json`.
- When adjusting any department to use live data, wire that department to the shared `CampusSystemDb` through that department's own context and schema, not through a new database or a shared cross-project context.
- Keep `InMemoryGuidanceRequestStore` and other in-memory scaffolding clearly marked as development-only until a durable department persistence layer is approved and implemented.
- Preserve security boundaries around student requests, counselor triage, and case notes.
- Read `DEVELOPER_SETUP.md` and `SERVICES.md` before service or API changes.
