# Core services

The service layer returns DTOs and `ServiceResult<T>` values; controllers should obtain the student identity from claims, never from request payloads.

- `AuthService`: short-lived JWT and rotating refresh cookies. Reads `Jwt:SigningKey` from configuration and is intended to sit behind rate limiting.
- `StudentRequestService`: validates DTOs, enforces student ownership, replays idempotent creates, and encodes safety-valve text before persistence.
- `CounselorTriageService`: lists the inbox, guards state transitions, translates EF concurrency exceptions to conflict results, and appends audit events.
- `AuditLogService`: append-only in-process audit log with read-only reporting queries. Replace its store with a database implementation for production.
- `CsvImportService`: strict CsvHelper roster map with dry-run validation before commit.
- `PiiMaskingService`: configurable email, phone, and SSN-like redaction for exports and summaries.
- `NotificationService`: outbound transport wrapped in Polly retry and circuit-breaker policies; failures are audited and returned as `false`.

Guidance request and refresh-token stores use MySQL `mydb` through `MySqlGuidancePersistenceStores`. Student directory reads use the shared `user` table through `GuidanceDbService`; student IDs are integer `user.id`. `AuditLogService` remains in-process and outbound message delivery is unavailable until a production transport is configured.

## Configuration and CI

Keep signing keys and connection strings out of JSON. Supply the shared `ConnectionStrings:DefaultConnection` with environment variables/user-secrets in development and a managed secret provider in production. The process variable is `ConnectionStrings__DefaultConnection`. Add this dependency check to CI:

```text
dotnet list package --vulnerable
```