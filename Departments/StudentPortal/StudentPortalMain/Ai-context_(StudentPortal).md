# StudentPortal AI Context V3

## Ownership

StudentPortal owns the student-facing dashboard, schedule, grades, guidance-request, profile, enrollment, financials, announcements, library-status, document-request, calendar, and support/helpdesk interfaces in this project. Keep changes local to `StudentPortalMain` unless a shared contract or platform change is required.

## How to edit this project

Use the task type to decide the correct layer:

- Front-end / interface work: edit only Razor Pages in `Pages/`, the shared layout in `Pages/Shared/_Layout.cshtml`, and styling in `wwwroot/css/site.css`. Keep requests, grades, profile, and schedule controls visual-only until a contract and auth review are approved.
- Back-end / data work: edit `Controllers/`, `Services/`, `Contracts/`, models, DTOs, `Program.cs`, and related server files only when the task explicitly requires grade persistence, schedule logic, profile updates, or request submission.
- AI model rule: if the request does not clearly ask for backend logic, assume it is a UI edit and do not add live student-record processing or API calls.

## AI maintenance manual

This file is the project guide for future AI sessions and developer handoffs. It should stay current as the project changes.

### When to update this file

Update this context whenever any of the following changes:

- a new Razor Page, folder, or route is added under `Pages/`
- a new controller, service, contract, model, or DTO is created
- a page starts depending on real grades, profiles, schedules, or guidance request data
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

This project is the student-facing shell for the campus experience. UI changes should stay in the presentation layer unless an approved contract and security review are already in place.

### Files to edit for UI changes

- Razor Pages in `Pages/`
- Shared layout in `Pages/Shared/_Layout.cshtml`
- Styling in `wwwroot/css/site.css`
- Supporting front-end assets in `wwwroot/`

### What not to add while editing the interface

- No live grade or schedule persistence
- No profile writes or enrollment changes
- No API calls that submit student records or guidance requests without an approved backend contract
- No direct reliance on raw request payload identity values for real authorization logic
- No live financials/payment processing or receipt generation
- No API calls that submit document requests or support/helpdesk tickets without an approved backend contract
- No live library-status data pulls until a contract with the Library system is approved
- No live announcements/notifications feed and no live calendar/events data pulls until a contract with the source system (registrar, faculty messaging, events office) is approved

### Approved UI behavior

- Keep request, profile, grade, schedule, enrollment, financials, document-request, and support controls visual-only until contract and auth work is approved.
- Preserve the current page structure, Bootstrap layout, and campus branding.
- Update mock data and UI states only; do not fabricate backend behavior.

## Department UI

The following Razor Pages are UI-only presentation placeholders with no database, service, API, or form-submit behavior:

- `/Dashboard`: schedule, grades, and notifications summary cards
- `/Schedule`: course schedule/calendar view
- `/Grades`: course list with grade badges
- `/Requests`: guidance request form aligned with Guidance's request shape
- `/Profile`: profile and settings form
- `/Enrollment`: enrollment status, assessment of fees, and enrollment history view
- `/Financials`: balance/statement of account, payment history, and receipt list view
- `/Announcements`: bulletin board and notifications list (enrollment, grades, faculty/registrar messages)
- `/Library`: library account status card (borrowed items, fines)
- `/DocumentRequests`: document request form and request-status tracker (COR, COE, good moral)
- `/Calendar`: school events and deadlines calendar view
- `/Support`: helpdesk/support ticket submission form and ticket list

The pages use the existing Bootstrap layout and local styles in `Pages/Shared/_Layout.cshtml` and `wwwroot/css/site.css`. Keep request, profile, grade, schedule, enrollment, financials, document-request, and support controls non-functional until approved contracts and authorization are available.

## Change Rules

- Students must be identified from authenticated claims when backend behavior is added; never trust identity fields from request payloads.
- Do not add persistence or service calls without approved contracts.
- Protect grades, profile data, and guidance requests with authorization and audit controls.
- Protect enrollment, financials, library status, document requests, announcements, calendar, and support tickets with the same authorization and audit controls once backend contracts are approved.
- Run `dotnet build StudentPortalMain.csproj` after UI changes.