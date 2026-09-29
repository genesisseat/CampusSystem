# Library Department Database Tables

## Current database state in this project

The Library project configures a SQL Server connection named `CampusSystemDb`, targeting `localhost,1433` and database `CampusSystemDb`. However, the current Library application does not define Library-specific EF Core entities, migrations, or a Library database context/table mapping.

The registered guidance request and refresh-token stores are in-memory implementations. Although SQL persistence classes exist in the project, `Program.cs` currently registers the in-memory stores, so those SQL tables are not the active persistence path for Library.

## Tables

No Library-owned database tables are defined by the current project source. Do not infer a `Books`, `Loans`, or `Reservations` table from the department name; a live schema inspection is required to determine whether such tables exist in the configured database.

## Shared services

The project contains shared guidance service models, but they are not currently persisted by the Library app's DI configuration. The SQL Server connection factory alone does not establish a Library-owned schema.

## Source references

- `Program.cs` configures the SQL Server connection and registers in-memory guidance stores.
- `appsettings.json` contains the `CampusSystemDb` target.
