# FacultyPortal AI Context

## Ownership

FacultyPortal owns the faculty-facing academic workflow interfaces in this project. Keep changes local to `FacultyPortalMain` unless a shared contract or platform change is required.

FacultyPortal is not a standalone system. Its roster, schedule, and gradebook queries use the shared MySQL database `mydb`; direct interdepartment API calls still require approved contracts.

## How to edit this project

Use the task type to decide the correct layer:

- Front-end / interface work: edit only Razor Pages in `Pages/`, the shared layout in `Pages/Shared/_Layout.cshtml`, and styling in `wwwroot/css/site.css`. Keep the output visual-only and non-functional unless a verified data contract exists.
- Back-end / data work: edit `Controllers/`, `Services/`, `Contracts/`, models, DTOs, `Program.cs`, and other server-side files only when the task explicitly requires real integration or business logic.
- AI model rule: preserve the existing MySQL-backed roster, schedule, and gradebook paths. For UI-only work, do not add persistence to the remaining placeholder workflows.
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
2. `System integration` — keep the Registrar and Student connections and their status current
3. `How to edit this project` — confirm whether the work is UI-only or backend
4. `Department UI` — add or remove page names and responsibilities
5. `Files to edit for UI changes` and `What not to add while editing the interface` — keep them aligned with the current files
6. `Change Rules` — add any new auth, validation, or contract requirements

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

Some Faculty pages are already database-backed. `/Roster`, `/Gradebook`, and `/Schedule` use `FacultyDbService`; assignments, attendance, and related controls remain placeholders unless an approved contract is added.

### Files to edit for UI changes

- Razor Pages in `Pages/`
- Shared layout in `Pages/Shared/_Layout.cshtml`
- Styling in `wwwroot/css/site.css`
- Supporting front-end assets in `wwwroot/`

### What not to add while editing the interface

- Do not replace the current Dapper/MySQL service with EF Core or a department-local database.
- Do not add live writes to assignment, attendance, or feedback placeholders without an approved contract.
- No auth or role enforcement logic that is not already in the app shell
- No form submissions that are not explicitly approved as UI-only placeholders
- Use shared MySQL `mydb` through `ConnectionStrings:DefaultConnection`, supplied through `ConnectionStrings__DefaultConnection` or a secret manager. Student references use integer `user.id`.
- Direct calls into another department's controllers, services, or API endpoints remain unapproved.

### Approved UI behavior

- Keep blank states, demo content, and mock tables until the owning team approves the backend contract.
- Preserve the current Razor Pages structure and namespace boundaries.
- Any button, form, or action should remain cosmetic unless a full data contract is already in place.

## Department UI

The following Razor Pages use or present Faculty workflows; roster, gradebook, and schedule data is queried from shared MySQL while remaining controls are placeholders:

- `/Roster`: class roster student table
- `/Gradebook`: assignment-by-student grade grid
- `/Attendance`: class and session attendance sheet
- `/Schedule`: faculty schedule/calendar view
- `/Assignments`: assignment list and create form (title, instructions, section, type, due date, max score, allowed file types, size/count limits, late rule, optional instructions file)
- `/AssignmentDetail`: one assignment's submissions table, submitted-file list, score, feedback and release-grade controls

There is no standalone Feedback page or sidebar tab. Teachers write feedback on each student's submission inside `/AssignmentDetail`. Do not add a Feedback page back.

The pages use the Student-style portal layout (sidebar, top bar, `portal-*` classes, NU blue and gold) defined in `Pages/Shared/_Layout.cshtml` and `wwwroot/css/site.css`, not Bootstrap. Assignments-specific helpers (`form-span`, `check-item`, `dropzone`, `file-list`, `card-footer-actions`) are at the end of `site.css`. Keep empty states and placeholder controls until the owning team defines the backend contract.

## System integration

FacultyPortal reads shared course, enrollment, student, grade, and Finance-clearance records from MySQL. It does not currently call Registrar or Student Portal APIs.

| Direction | What moves |
|---|---|
| Registrar to Faculty | Sections assigned to the teacher, enrollment (class rosters), schedule |
| Student to Faculty | Assignment submissions (including files), student IDs |
| Faculty to Student | Assignments and due dates, grades, feedback, attendance, announcements |
| Faculty to Registrar | Final grades and attendance |

Rules for these connections:

- Continue reading shared course, enrollment, student, and clearance data from the operational tables; do not duplicate these records in a Faculty database.
- Grade writes currently update the shared `grades` table. Validate schema or workflow changes against Student Portal and Registrar consumers.
- Faculty owns `Assignments`, `Grades` and `Feedback`; Student reads them through the contract. Students only see grades after the teacher releases them.
- A teacher must be linked to a section by ID (the Registrar `class_offerings` table currently has only `instructor_name` text).
- Affected pages: `/Roster` and `/Schedule` (Registrar), `/Assignments`, `/AssignmentDetail`, `/Gradebook` (Student and Registrar), `/Attendance` (Registrar).
- Open item: instructors are represented by `instructor_name` text on `class_offerings`; an authenticated faculty-to-offering ID relationship is still needed.

## Planned backend (not approved yet)

The Assignments pages are UI-only with demo data. When the backend starts, it needs an approved contract with Student and Registrar first. Planned shape:

- Faculty's future assignments/submissions/feedback feature still needs an approved contract and storage design; do not create a Faculty-only database or duplicate shared student/grade records.
- Student's current `student_assignments` table is a per-student placeholder (no section link, no files, no feedback) and must be replaced by the contract.
- Files are stored outside `wwwroot` under random names, validated by content, served through an authorised endpoint.
- Open items: how a teacher is linked to a section, and where assignment/submission files are stored.

## Change Rules

- Do not add persistence or service calls to these pages without an approved contract.
- Any Registrar or Student connection must follow the contract in `System integration`.
- Preserve existing Razor Pages behavior and department namespace.
- Add validation and authorization before enabling grade, attendance, feedback, roster, or schedule actions.
- Run `dotnet build FacultyPortalMain.csproj` after UI changes.
- Assignments buttons and fields stay cosmetic (no `<form>`, no posts) until the contract is approved.
