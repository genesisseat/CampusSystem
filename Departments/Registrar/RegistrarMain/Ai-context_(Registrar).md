# Registrar AI Context

## Ownership

Registrar owns course browsing, registration, academic records, verification, and records-request interfaces in this project. Keep changes local to `RegistrarMain` unless a shared contract or platform change is required.

## How to edit this project

Use the task type to decide the correct layer:

- Front-end / interface work: edit only Razor Pages in `Pages/`, the shared layout in `Pages/Shared/_Layout.cshtml`, and styling in `wwwroot/css/site.css`. Keep registration and records flows visual-only until an approved contract exists.
- Back-end / data work: edit `Controllers/`, `Services/`, `Contracts/`, models, DTOs, `Program.cs`, and related server files only when the task explicitly requires course, enrollment, transcript, or record processing.
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

## Interface-only editing guidance

This project is UI-first and intentionally does not include live registration or record processing. Interface edits should remain presentation-focused and avoid backend wiring.

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

The following Razor Pages are UI-only presentation placeholders with no database, service, API, or form-submit behavior:

- `/Courses`: course catalog and browse page
- `/Registration`: registration and schedule builder
- `/Transcript`: semester-grouped transcript view
- `/Verification`: enrollment verification request form
- `/Records`: records request and status tracker

The pages use the existing Bootstrap layout and local styles in `Pages/Shared/_Layout.cshtml` and `wwwroot/css/site.css`. Keep registration, verification, transcript, and records controls non-functional until the owning team defines contracts and authorization rules.

## Change Rules

- Do not add persistence or service calls without approved registrar contracts.
- Protect academic records and enrollment verification with authorization and audit controls before backend wiring.
- Preserve existing Razor Pages behavior and department namespace.
- Run `dotnet build RegistrarMain.csproj` after UI changes.

##