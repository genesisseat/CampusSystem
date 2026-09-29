# Registrar Department Database Tables

This document summarizes the shared MySQL tables used by Registrar's pages and API.

## Connection

- Connection name: `DefaultConnection`
- Database: shared `mydb`
- Student identity: integer `user.id`

## Shared tables used by Registrar

### `subjects` and `class_offerings`

| Table | Important columns |
|---|---|---|
| `subjects` | `id`, `subject_code`, `subject_name`, `units`, curriculum metadata |
| `class_offerings` | `id`, `subject_id`, `section_code`, `school_year`, `semester`, `capacity`, `slots_taken`, `status` |

The course API lists open offerings. Registration submits an offering ID, not a subject ID or client-supplied semester.

### `enrollments` and `enrolled_subjects`

`enrollments.student_id` references shared `user.id`; `enrolled_subjects` links an enrollment to a `class_offerings` row. Seat counts are adjusted in the same transaction as add/drop operations.

### `grades`

Transcript rows join `grades` through `enrolled_subjects`, `enrollments`, `class_offerings`, and `subjects`.

### `document_requests`

Records requests and enrollment verification requests are both stored in this shared table. Registrar verifies or creates its schema at startup; verification requests use `document_type = 'Enrollment Verification'`.

## Legacy source files

The old `RegistrarDbContext`, SQL Server migrations, and `registrar.*` models remain in the repository but are no longer registered or used at runtime. Do not apply those migrations or use them for new features.

## Notes

- Registrar bootstraps only `document_requests` with `CREATE TABLE IF NOT EXISTS`; it does not create or alter student, course, enrollment, offering, or grade tables.
- Student-owned API requests resolve the authenticated `StudentId` claim to `user.id` or `user.student_id_number`.
