# Health and repair

This folder contains the health check launcher for this department project.

## Health check

From this project directory, run:

```powershell
.\HealthAndRepair\Check-GuidanceServices.ps1
```

The check verifies:

- Required files under `Contracts` and `Services`.
- Project namespace imports.
- Dependency injection registrations in `Program.cs`.
- Required NuGet package references.

To include compilation, run:

```powershell
.\HealthAndRepair\Check-GuidanceServices.ps1 -Build
```

A healthy project ends with `HEALTHY`. Missing items produce `MISSING ITEMS` and the script exits with code 1.

## Repair

Run the central installer from the workspace root to restore or repair all departments:

```powershell
cd X:\CampusSystem
.\Install-GuidanceServices.ps1 -Department FacultyPortal
```

Preview the repair first:

```powershell
.\Install-GuidanceServices.ps1 -Department FacultyPortal -WhatIf
```

The installer backs up `Program.cs` and existing service files in `.guidance-services-backup` before replacing them. Review the backup before deleting it. The installed stores and outbound transport are development scaffolding and must be replaced with production implementations before deployment.

## Workspace-wide check

From `X:\CampusSystem`:

```powershell
.\Check-GuidanceServices.ps1 -AllDepartments -Build
```

If scripts are blocked by execution policy, run the command with `-ExecutionPolicy Bypass` through PowerShell. Do not put signing keys or connection strings in source-controlled JSON files.

## Faculty system progress

Faculty module of CampusSystem. It must integrate with the Student and Registrar systems.

**Current status:** the Faculty portal is UI-only with demo data. It has no backend or database of its own yet, and it is not connected to the Registrar or Student systems. The data-flow table below is the planned integration, and each connection needs a contract approved by the owning team first.

### Data flow

| Direction | What moves | Status |
|---|---|---|
| Registrar to Faculty | Sections assigned to the teacher, enrollment (class rosters), schedule | In diagram |
| Student to Faculty | Assignment submissions, student IDs | In diagram |
| Faculty to Student | Grades, feedback, attendance, announcements | In diagram |
| Faculty to Registrar | Final grades and attendance | Not in diagram, needs adding |
| Faculty and Payroll | Teaching load and employment type out, payslips (read-only) in | Not in diagram, needs deciding |

Faculty owns: `Faculty`, `Teacher`, `Authentication`, `Assignments`, `Grades`, `Feedback`, `Attendance_Sessions`, `Attendance_Record`, `Announcement`. Registrar and Student data is read-only in Faculty. Ownership is an assumption to confirm. `Feedback` stays a data table tied to a submission, but has no page of its own.

### Design

- [x] Faculty data-flow diagram (ERD) drafted
- [ ] Confirm data ownership with the Registrar and Student teams
- [ ] Fix data model: one identity key for Faculty/Teacher, `Attendance_Record` needs `Status` instead of repeated times and topic, remove Schedule/Sections overlap, rename `ViewUpdate_Grades` to `Grades`, fix `Statuis`, drop `Announcement.PostedBy`
- [ ] Add the Faculty to Registrar return flow (final grades, attendance)
- [ ] Decide payroll/HR integration: Payroll owns pay, portal shows payslips pulled on request, no salary or bank details in the faculty database, hours from `Attendance_Sessions` only if pay is hourly
- [ ] Decide on other missing items: leave requests, time records, evaluations, documents

### Front end

FacultyPortal is an ASP.NET Core Razor Pages project. The interface is UI-only until a backend contract is approved: no database, service, API, or form-submit behavior.

- [x] Standalone HTML prototype (`faculty-portal.html`) used to plan the screens
- [x] Faculty layout, `site.css` and pages rebuilt on the Student portal theme (sidebar, top bar, `portal-*` classes, NU blue and gold)
- [x] UI-only pages drafted: Dashboard (`Index`), `/Roster`, `/Gradebook`, `/Attendance`, `/Schedule`
- [x] Assignments pages drafted with demo data: `/Assignments` (list and create) and `/AssignmentDetail` (submissions, grading, feedback), plus an Assignments sidebar item
- [x] Feedback tab removed: teachers write feedback per submission in `/AssignmentDetail`. Delete `Pages/Feedback.cshtml` from the project
- [ ] Copy into the project (back up first): `Pages/Shared/_Layout.cshtml`, the `Pages/*.cshtml` files (including `Assignments` and `AssignmentDetail`, without `Feedback`), `wwwroot/css/site.css`, `wwwroot/img/logo3.png`
- [ ] Run `dotnet build FacultyPortalMain.csproj`
- [ ] Open each page in the browser and check it looks right
- [x] Update the AI context file: pages now use the Student-style portal layout in `site.css`, not Bootstrap; it also lists the Assignments pages and the Registrar and Student connections

### Back end

Only start once the owning teams approve a contract.

- [ ] Get approved Student and Registrar contracts (rosters, schedule, submissions). No direct calls into other departments' controllers, services or endpoints until then.
- [ ] Create `FacultyPortalDbContext`, models and migrations inside this project. Do not add `DbSet`s to `CampusSystem.Data` or another department's context.
- [ ] Assignments backend: contract with Student for assignments, multi-file submissions, grades and feedback (replaces the per-student `student_assignments` placeholder), then file storage and upload validation
- [ ] Confirm the database engine (Faculty references SQL Server EF Core, Student uses MySQL) and link teachers to sections by ID
- [ ] Add validation and authorization before enabling grade, attendance, feedback, roster or schedule actions
- [ ] Replace demo content with real data, one page at a time
- [ ] Health check ends with `HEALTHY`
