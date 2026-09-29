# Library Department Database Tables

## Current database state in this project

The Library project requires the `ConnectionStrings__DefaultConnection` environment variable for MySQL access to shared database `mydb`. Credentials are not stored in this project's settings files. `LibraryDbService.EnsureSchemaAsync` runs at startup and creates its bootstrap tables if they do not exist.

The Library pages remain UI placeholders. The database helper's account and clearance methods are not currently called by page handlers or controllers. Guidance request and refresh-token stores registered by the app remain in-memory scaffolding and are not Library circulation storage.

## Tables

### `library_accounts`

| Column | MySQL type | Notes |
|---|---|---|
| id | int | Auto-increment primary key |
| student_id | int | Shared `user.id`; unique per account |
| has_overdue | tinyint(1) | Overdue indicator |
| fine_amount | decimal(10,2) | Current fine balance |
| fine_paid | tinyint(1) | Whether the fine is paid |
| updated_at | datetime | Updated automatically |

### Shared `student_clearance`

Library bootstrap creates this table if absent, with a unique key on student, department, school year, and semester. Finance and Student Portal also use this shared table; confirm their deployed schema before applying incompatible schema changes.

No `library_books`, `book_loans`, or `library_reservations` table is currently defined. Do not infer live catalog or circulation persistence from the placeholder pages.

## Shared services

`LibraryDbService` reads student identities from the shared `user` table and provides methods for Library account and clearance writes. These methods are not yet connected to the page workflows; there is no live catalog, loan, reservation, or fine-payment flow.

## Source references

- `Program.cs` configures the MySQL service and invokes its best-effort schema bootstrap.
- `LibraryDbService.cs` defines the bootstrap tables and currently unused data methods.
- `appsettings.json` contains the `DefaultConnection` target for `mydb`.
