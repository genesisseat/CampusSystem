# Shared MySQL Database

## Decision

All department applications use the same operational MySQL database, `mydb`. They must use the common `ConnectionStrings:DefaultConnection` key and the shared integer student identity `user.id`. Do not add another application database, retain a SQL Server runtime path, or create a department-specific student ID.

## Configuration

Supply `ConnectionStrings__DefaultConnection` through the process environment or a secret manager. The connection string has this shape:

```text
Server=<mysql-host>;Port=3306;Database=mydb;Uid=<mysql-user>;Pwd=<secret>;
```

For a local PowerShell session, set the variable before starting an app:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=<mysql-host>;Port=3306;Database=mydb;Uid=<mysql-user>;Pwd=<secret>;"
dotnet run --project .\Departments\Registrar\RegistrarMain\RegistrarMain.csproj
```

The variable is inherited by child processes in that PowerShell session. Never put the real value in committed `appsettings*.json`, scripts, logs, or documentation. Applications fail startup when a required connection is missing or schema initialization fails.

## Department Data Paths

| Department | Current MySQL use | Schema/bootstrap notes |
|---|---|---|
| Faculty Portal | Reads students, offerings, rosters, grades, and Finance clearance; writes grades through `FacultyDbService`. | Uses shared academic and clearance tables. Some ancillary workflows remain placeholders. |
| Finance | Uses shared `user`, `student_payments`, `fee_assessments`, and `student_clearance`. | `FinanceDbService` verifies/creates payment, assessment, and clearance tables. Payment writes reject unknown student numbers; `UpdateClearanceStatusAsync` still contains a legacy fallback to `user.id = 5` and must be fixed before that path is used operationally. |
| Guidance | Reads active students from `user`; stores Guidance requests and refresh tokens through MySQL. | `GuidanceDbService` bootstraps Guidance-owned request, appointment, note, and refresh-token tables. |
| Library | `LibraryDbService` reads shared student IDs and can write account/clearance state. | Bootstraps `library_accounts`; catalog, loan, reservation, and fine pages are still placeholders and are not yet connected to these methods. |
| Registrar | Razor pages and APIs use the shared academic, grades, student, and document-request tables. | Bootstraps `document_requests`; it does not create the shared student, subject, offering, enrollment, or grade tables. Do not apply its legacy SQL Server EF migrations. |
| Student Portal | Uses MySQL `StudentPortalDbService` for student-facing reads and writes. | Uses existing shared tables; keep student IDs as `user.id`. Some page workflows remain incomplete. |
| Testing | Uses MySQL for QA workflows and inspects the database selected by `DefaultConnection`. | Verify the target database and table before any modifying test operation. |

## Shared Table Conventions

- `user.id` is the numeric primary key used for cross-department student references; `user.student_id_number` is the human-readable student number.
- Shared academic data includes `subjects`, `class_offerings`, `enrollments`, `enrolled_subjects`, and `grades`.
- Student document and verification requests use `document_requests`.
- Finance payments and clearance use `student_payments` and `student_clearance`.
- Departments may own additional tables inside `mydb`, but they must coordinate schema changes and preserve compatibility with existing records.
- Use idempotent schema setup. Do not drop or recreate shared tables as part of routine app startup.

## Registrar API Contract

The Registrar browser client and API now use shared records:

- Course listings are based on `subjects` joined to open `class_offerings` and include the real `offeringId`.
- Registration submits `{ offeringId }`, resolves the authenticated student to `user.id`, and transactionally updates `enrollments`, `enrolled_subjects`, and seat counts.
- Transcript reads join `grades` through enrollment and offering tables.
- Records and enrollment-verification requests share `document_requests`.

## Verification

Run the structural audit and builds from the repository root:

```powershell
.\Check-GuidanceServices.ps1 -ProjectPath . -AllDepartments -Build
```

Add `-RequireDatabaseConfiguration` to fail the check unless `ConnectionStrings__DefaultConnection` is set in the current process. A structural build does not prove network/database access; use read-only smoke checks against each deployed environment before enabling workflows. Avoid test inserts into the operational database unless they are explicitly approved and safely cleaned up.

The current integration status is not full feature parity: several department pages still contain mock or placeholder workflows even though their applications share the MySQL connection. Do not describe those workflows as synchronized until they are connected to shared records and tested end to end.
