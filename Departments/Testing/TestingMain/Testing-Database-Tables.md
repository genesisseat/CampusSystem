# Testing Department Database Tables

## Database connection

The Testing application uses MySQL `DefaultConnection`, configured for host `100.98.41.69`, port `3306`, database `mydb`.

## Schema behavior

Testing is a database inspection/maintenance interface, not an application with a fixed set of owned tables. Its page runs `SHOW DATABASES`, then `SHOW TABLES FROM <selected database>`, and reads selected tables dynamically. It also has generic row-update and row-delete operations.

Therefore, the project does not declare a canonical Testing table list or table-column schema. The tables shown in the UI depend on the MySQL server and the database selected at runtime.

## Runtime-discovered data

- Databases: discovered using MySQL `SHOW DATABASES`.
- Tables: discovered using `SHOW TABLES FROM <database>`.
- Columns and rows: read dynamically from the chosen table.
- Updates/deletes: use a detected ID/primary-key column on the chosen table.

## Important operational note

This tool can modify or delete records in whichever database/table is selected. Verify the selected server, database, and table before submitting a change. To produce an exact schema inventory, connect to the target MySQL server and inspect the live schema; this source code alone cannot specify which remote tables currently exist.

## Source references

- `Pages/Index.cshtml.cs` contains the database discovery and dynamic data operations.
- `appsettings.json` provides the `DefaultConnection`.
