# Core services

The service layer returns DTOs and `ServiceResult<T>` values; controllers should obtain the student identity from claims, never from request payloads.

- `AuthService`: short-lived JWT and rotating refresh cookies. Reads `Jwt:SigningKey` from configuration and is intended to sit behind rate limiting.
- `StudentRequestService`: validates DTOs, enforces student ownership, replays idempotent creates, and encodes safety-valve text before persistence.
- `CounselorTriageService`: lists the inbox, guards state transitions, translates EF concurrency exceptions to conflict results, and appends audit events.
- `AuditLogService`: append-only in-process audit log with read-only reporting queries. Replace its store with a database implementation for production.
- `CsvImportService`: strict CsvHelper roster map with dry-run validation before commit.
- `PiiMaskingService`: configurable email, phone, and SSN-like redaction for exports and summaries.
- `NotificationService`: outbound transport wrapped in Polly retry and circuit-breaker policies; failures are audited and returned as `false`.

`InMemoryGuidanceRequestStore` and `InMemoryRefreshTokenStore` are development scaffolding. Replace them with EF Core/Identity-backed implementations before production.

## Configuration and CI

Keep signing keys and connection strings out of JSON. Use `dotnet user-secrets` during development and an Azure Key Vault configuration provider in production. Add this dependency check to CI:

```text
dotnet list package --vulnerable
```