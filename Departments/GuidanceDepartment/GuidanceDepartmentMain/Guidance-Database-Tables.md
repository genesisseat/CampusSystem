# Guidance Department Database Tables

This reference describes Guidance's MySQL tables and its reads from the shared campus database.

## Database connection

- Connection name: `DefaultConnection`
- Configured target: MySQL database `mydb`.
- Guidance uses the shared `user` table and creates its own tables with `GuidanceDbService.EnsureSchemaAsync` at startup.

## Tables

### Shared `user`

| Column | MySQL type | Notes |
|---|---|---|
| id | int | Shared numeric student identity |
| student_id_number | varchar | Student number |
| name | varchar | Full name |
| email | varchar | Email address |

### `guidance_requests`

| Column | MySQL type | Notes |
|---|---|---|
| id | char(36) | Primary key |
| student_id | int | References shared `user.id` by convention |
| subject, details | varchar / text | Request content |
| safety_valve_text | text, nullable | Optional safety text |
| urgency, status | int | Enum values |
| assigned_counselor_id | char(36), nullable | Optional assigned counselor |
| idempotency_key | varchar(191), nullable | Unique per student when provided |
| row_version | binary(8) | Optimistic concurrency value |

### `refresh_tokens`

| Column | MySQL type | Notes |
|---|---|---|
| token | varchar(128) | Primary key |
| subject | varchar(255) | Token subject |
| expires_at | datetime(6) | Expiry timestamp |

### Other Guidance tables

- `guidance_notes` and `guidance_appointments` are created at startup for Guidance-owned records.
- `student_clearance` is shared and is not created by Guidance.

## Notes

- Schema creation is best-effort at startup; database errors are logged and the app continues running.
- Student identifiers in shared workflows use integer `user.id`, not a department-local GUID.
