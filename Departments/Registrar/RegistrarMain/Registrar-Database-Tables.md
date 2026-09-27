# Registrar Department Database Tables

This document summarizes the database tables used by the Registrar department application, based on the EF Core models in the Registrar project.

## 1) Registrar schema tables

### `registrar.Courses`

| Column | Type | Notes |
|---|---|---|
| Id | int | Primary key |
| Code | nvarchar | Course code, e.g. `CS101` |
| Title | nvarchar | Course title |
| Credits | int | Credit value |

### `registrar.Enrollments`

| Column | Type | Notes |
|---|---|---|
| Id | int | Primary key |
| StudentId | uniqueidentifier | FK to `dbo.Students.Id` |
| CourseId | int | Course reference |
| Semester | nvarchar | Registration term |
| RowVersion | rowversion | Concurrency token |

### `registrar.TranscriptEntries`

| Column | Type | Notes |
|---|---|---|
| Id | int | Primary key |
| StudentId | uniqueidentifier | FK to `dbo.Students.Id` |
| Semester | nvarchar | Term for the grade |
| CourseId | int | Course reference |
| Grade | nvarchar | Final grade value |

### `registrar.VerificationRequests`

| Column | Type | Notes |
|---|---|---|
| Id | int | Primary key |
| StudentId | uniqueidentifier | FK to `dbo.Students.Id` |
| Status | nvarchar | Default: `Pending` |
| RequestedAt | datetime | Request timestamp |

### `registrar.RecordsRequests`

| Column | Type | Notes |
|---|---|---|
| Id | int | Primary key |
| StudentId | uniqueidentifier | FK to `dbo.Students.Id` |
| DocumentType | nvarchar | Request type |
| Status | nvarchar | Default: `Pending` |
| RequestedAt | datetime | Request timestamp |

## 2) Shared student table

### `dbo.Students`

| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | Primary key |
| StudentNumber | nvarchar | Student identifier number |
| FullName | nvarchar | Full student name |
| Email | nvarchar | Email address |

## 3) Relationship summary

- `registrar.Enrollments.StudentId` -> `dbo.Students.Id`
- `registrar.TranscriptEntries.StudentId` -> `dbo.Students.Id`
- `registrar.VerificationRequests.StudentId` -> `dbo.Students.Id`
- `registrar.RecordsRequests.StudentId` -> `dbo.Students.Id`
- `registrar.Enrollments.CourseId` -> `registrar.Courses.Id`

## 4) Notes

- The registrar context is defined in `RegistrarDbContext`.
- The `Students` table is mapped from the shared `CampusSystem.Data.Models.Student` model and is stored in the `dbo` schema.
- Registrar-specific tables are mapped in the `registrar` schema.

This file reflects the current EF Core model definitions in the project and is intended as a quick database reference for the registrar department.
