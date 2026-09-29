# GuidanceDepartment AI Context


## Ownership


GuidanceDepartment owns the student-support and counselor workflow interfaces in this project. This project uses static HTML under `wwwroot` alongside its API and controllers.


## How to edit this project


Use the task type to decide the correct layer:


- Front-end / interface work: edit static pages in `wwwroot/`, shared styling in `wwwroot/styles.css`, and navigation/layout structure in the HTML files. Keep forms and workflow cards visual placeholders unless an approved contract exists.
- Back-end / data work: edit `Controllers/`, `Services/`, `Contracts/`, DTOs, models, `Program.cs`, and other server files only when the task explicitly requires guidance data, case note logic, or a live API contract.
- AI model rule: if the request does not clearly include backend requirements, assume it is an interface edit and do not add persistence, API calls, or live student-record submission.
- Department DB wiring: use shared MySQL `mydb` through `ConnectionStrings:DefaultConnection` and the Dapper/MySqlConnector services in this project; supply credentials via environment or a secret manager.


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
- Do not create a separate Guidance database or add Guidance tables into `CampusSystem.Data`; use the shared MySQL database and the `user.id` integer for student references.
- Direct calls into another department's controllers, services, or API endpoints remain unapproved.


### Approved UI behavior


- Keep forms, cards, and triage lanes as visual placeholders until the backend contract is approved.
- Preserve the static-file structure and security boundaries around sensitive student data.
- Use the existing `StudentRequestService` DTO shape only as a design reference; do not connect the form to real storage without approval.


## Department UI


Guidance reads active student rows from the shared MySQL `user` table. `MySqlGuidancePersistenceStores` stores requests and refresh tokens; `GuidanceDbService` bootstraps Guidance-owned request, appointment, note, and token tables. These services require `ConnectionStrings__DefaultConnection`; schema initialization and shared student reads fail visibly on database errors.

The legacy EF Core context and SQL Server migration files are inactive and excluded from compilation. Do not re-enable or apply them. The static UI remains partly presentation-only; keep sensitive case-note and counselor workflows behind explicit authorization.

- `/index.html`: department overview and core module workspace hub
- `/dashboard.html`: counselor operations dashboard with priority queue and live stats
- `/students.html`: registered student directory with live campus database feed
- `/triage.html`: counselor intake queue and 3-stage triage Kanban board
- `/appointments.html`: appointment calendar and consultation slot picker
- `/case-notes.html`: restricted-access confidential case notes panel
- `/monitoring.html`: counselor-only student follow-up register with filters and caseload tracking
- `/request.html`: guidance and counseling intake request form
- `/resources.html`: student and counselor advising and wellness resources catalog
- `/admin.html`: department routing, escalation thresholds, and inter-department referral settings

The Guidance UI is fully styled in the National University Lipa institutional design system (Navy `#0a1d4d`, Gold `#f2b807`, Inter typography, left sidebar navigation shell, status topbar with Academic Year indicators, and clean card/table layouts) aligned with the Registrar and Finance department systems. The pages reuse `wwwroot/styles.css` and are linked from the Guidance sidebar navigation. `wwwroot/prototype.js` supplies local fixture data and preview-only interactions for the dashboard, student monitoring register, triage queue, request form, case notes, and appointment slots. The monitoring page shows masked student references, programme, support signal, request, follow-up date, and assigned counselor; it is not a student-record system. The request form should remain aligned with the `StudentRequestService` DTO shape, but it must not submit until the owning team connects it to the approved contract. Case notes require real authorization and restricted storage before use. The dashboard, monitoring, and admin pages are visual prototypes only and must not be treated as department-scoped authorization or configuration.


## Change Rules


- Preserve the static-file/controller architecture; do not convert to Razor Pages without an explicit decision.
- Do not add persistence or service calls to placeholder pages without an approved contract.
- Use shared MySQL `mydb` through `ConnectionStrings:DefaultConnection`; never store its credentials in committed settings.
- Student identity is the shared integer `user.id`, not the legacy GUID identity model.
- Guidance requests and refresh tokens use MySQL stores; audit logging and outbound notifications still need durable/production implementations.
- Preserve security boundaries around student requests, counselor triage, and case notes.
- Read `DEVELOPER_SETUP.md` and `SERVICES.md` before service or API changes.
