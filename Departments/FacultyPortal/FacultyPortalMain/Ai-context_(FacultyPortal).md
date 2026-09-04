# FacultyPortal AI Context

## Ownership

FacultyPortal owns the faculty-facing academic workflow interfaces in this project. Keep changes local to `FacultyPortalMain` unless a shared contract or platform change is required.

## How to edit this project

Use the task type to decide the correct layer:

- Front-end / interface work: edit only Razor Pages in `Pages/`, the shared layout in `Pages/Shared/_Layout.cshtml`, and styling in `wwwroot/css/site.css`. Keep the output visual-only and non-functional unless a verified data contract exists.
- Back-end / data work: edit `Controllers/`, `Services/`, `Contracts/`, models, DTOs, `Program.cs`, and other server-side files only when the task explicitly requires real integration or business logic.
- AI model rule: if the request does not clearly ask for backend logic, assume it is an interface edit and do not add persistence, API calls, or database access.
## AI maintenance manual

This file is the project guide for future AI sessions and developer handoffs. It should stay current as the project changes.

### When to update this file

Update this context whenever any of the following changes:

- a new Razor Page, folder, or route is added under `Pages/`
- a new controller, service, contract, model, or DTO is created
- a page starts depending on real data or a new business workflow is introduced
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

The interface in this project is intentionally frontend-only. When modifying the faculty UI, edit only the presentation layer and keep backend integration out of scope unless an approved contract exists.

### Files to edit for UI changes

- Razor Pages in `Pages/`
- Shared layout in `Pages/Shared/_Layout.cshtml`
- Styling in `wwwroot/css/site.css`
- Supporting front-end assets in `wwwroot/`

### What not to add while editing the interface

- No database access or EF Core calls
- No controller logic that persists or queries data
- No service calls that change live records
- No auth or role enforcement logic that is not already in the app shell
- No form submissions that are not explicitly approved as UI-only placeholders

### Approved UI behavior

- Keep blank states, demo content, and mock tables until the owning team approves the backend contract.
- Preserve the current Razor Pages structure and namespace boundaries.
- Any button, form, or action should remain cosmetic unless a full data contract is already in place.

## Department UI

The following Razor Pages are UI-only presentation placeholders with no database, service, API, or form-submit behavior:

- `/Roster`: class roster student table
- `/Gradebook`: assignment-by-student grade grid
- `/Attendance`: class and session attendance sheet
- `/Feedback`: assignment feedback and comment panel
- `/Schedule`: faculty schedule/calendar view

The pages use the existing Bootstrap layout and local styles in `Pages/Shared/_Layout.cshtml` and `wwwroot/css/site.css`. Keep empty states and placeholder controls until the owning team defines the backend contract.

## Change Rules

- Do not add persistence or service calls to these pages without an approved contract.
- Preserve existing Razor Pages behavior and department namespace.
- Add validation and authorization before enabling grade, attendance, feedback, roster, or schedule actions.
- Run `dotnet build FacultyPortalMain.csproj` after UI changes.
