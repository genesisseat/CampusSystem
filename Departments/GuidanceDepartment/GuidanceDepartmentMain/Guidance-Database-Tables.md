# Guidance Department Database Tables

This reference describes the Guidance department's SQL Server EF Core model and migration snapshot.

## Database connection

- Connection name: `CampusSystemDb`
- Configured target: SQL Server at `localhost,1433`, database `CampusSystemDb`.
- Guidance-owned tables are in the `guidance` schema. The shared student table is in `dbo`.

## Tables

### `dbo.Students`

| Column | SQL Server type | Notes |
|---|---|---|
| Id | uniqueidentifier | Primary key |
| StudentNumber | nvarchar(max) | Student number |
| FullName | nvarchar(max) | Full name |
| Email | nvarchar(max) | Email address |

### `guidance.GuidanceRequests`

| Column | SQL Server type | Notes |
|---|---|---|
| Id | uniqueidentifier | Primary key |
| StudentId | uniqueidentifier | Student identity |
| Subject | nvarchar(max) | Required request subject |
| Details | nvarchar(max) | Required request details |
| SafetyValveText | nvarchar(max), nullable | Optional safety text |
| Urgency | int | `RequestUrgency` enum value |
| Status | int | `RequestStatus` enum value |
| AssignedCounselorId | uniqueidentifier, nullable | Optional assigned counselor |
| IdempotencyKey | nvarchar(max), nullable | Request de-duplication key |
| RowVersion | varbinary(max) | Concurrency value |

### `guidance.RefreshTokens`

| Column | SQL Server type | Notes |
|---|---|---|
| Token | nvarchar(450) | Primary key |
| Subject | nvarchar(max) | Token subject |
| ExpiresAt | datetimeoffset | Expiry timestamp |

## Relationships

- `GuidanceRequests.StudentId` identifies a student in `dbo.Students`; the current EF model does not configure a foreign-key constraint.
- `RefreshTokens` is keyed by `Token`.

## Notes

- These are the current EF Core mappings and migration snapshot; see `Data/GuidanceDbContext.cs` and `Migrations/GuidanceDbContextModelSnapshot.cs`.
- The configured SQL Server service must be reachable at the connection string target for these persistent tables to be available.
