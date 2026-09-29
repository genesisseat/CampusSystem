# Finance Department Project Documentation

**Scope:** `Departments/Finance/FinanceMain` only  
**Reviewed:** 2026-09-29
**Application:** ASP.NET Core Razor Pages, target framework `net10.0`

This document is the programmer handoff and maintenance guide for the Finance department. Paths are relative to the repository root unless marked as an absolute location. Keep Finance changes inside this department unless a shared contract or platform change has been approved.

## Current state

Finance pages call assessment, payment, clearance, and request services. `FinanceDataStore` still supplies demo assessment and ledger state; payment and clearance services can write to shared MySQL `mydb`. The department is therefore partly integrated, not fully synchronized.

The app also retains copied student-request/counselor scaffolding that is separate from its finance workflows. Do not treat those in-memory stores as Finance persistence.

## Project map

| Area | Location | Responsibility |
|---|---|---|
| Application startup | `Departments/Finance/FinanceMain/Program.cs` | Service registration and HTTP/Razor pipeline |
| Finance pages | `Departments/Finance/FinanceMain/Pages/` | Razor UI and page routes |
| Shared page shell | `Departments/Finance/FinanceMain/Pages/Shared/_Layout.cshtml` | Navigation, Bootstrap, scripts, footer |
| Page model handlers | `Departments/Finance/FinanceMain/Pages/*.cshtml.cs` | Server-side page handlers where present |
| Contracts and validation | `Departments/Finance/FinanceMain/Contracts/` | DTOs, result types, enums, validators |
| Services | `Departments/Finance/FinanceMain/Services/` | Backend workflows and infrastructure utilities |
| CSS | `Departments/Finance/FinanceMain/wwwroot/css/site.css` | Finance visual theme and components |
| JavaScript | `Departments/Finance/FinanceMain/wwwroot/js/site.js` | Browser behavior loaded by the layout |
| Configuration | `Departments/Finance/FinanceMain/appsettings.json` and `appsettings.Development.json` | Environment configuration |
| Project dependencies | `Departments/Finance/FinanceMain/FinanceMain.csproj` | .NET version and NuGet/project references |

## Frontend editing guide

### Page locations and responsibilities

| Route | File | Current behavior |
|---|---|---|
| `/` | `Departments/Finance/FinanceMain/Pages/Index.cshtml` | Finance Center overview and links/context |
| `/Billing` | `Departments/Finance/FinanceMain/Pages/Billing.cshtml` | Empty billing/invoice table, search/status controls, export placeholder |
| `/Invoice` | `Departments/Finance/FinanceMain/Pages/Invoice.cshtml` | Placeholder invoice detail, empty line items, PDF button placeholder |
| `/Payments` | `Departments/Finance/FinanceMain/Pages/Payments.cshtml` | Empty payment history table and statement button placeholder |
| `/Pay` | `Departments/Finance/FinanceMain/Pages/Pay.cshtml` | Non-submitting amount/method preview form |
| `/Aid` | `Departments/Finance/FinanceMain/Pages/Aid.cshtml` | Placeholder aid totals and pending status |
| `/Privacy` | `Departments/Finance/FinanceMain/Pages/Privacy.cshtml` | Privacy page |
| error page | `Departments/Finance/FinanceMain/Pages/Error.cshtml` | Error display |

### What to edit for UI-only work

1. Edit the relevant `.cshtml` file under `Pages/` for labels, layout, table columns, empty states, and page content.
2. Edit `Pages/Shared/_Layout.cshtml` for navigation, shared scripts, page title structure, or footer changes.
3. Edit `wwwroot/css/site.css` for colors, spacing, responsive behavior, forms, tables, and shared components.
4. Edit `wwwroot/js/site.js` only for browser-side behavior. Confirm the file is loaded by `_Layout.cshtml` before adding page-specific behavior.
5. Add a page-specific `.cshtml.cs` model beside a page only when the page needs server-side binding or a handler. Existing page-model files are `Index.cshtml.cs`, `Privacy.cshtml.cs`, and `Error.cshtml.cs`.

### UI rules

- Preserve the existing Bootstrap structure and local CSS variables.
- Treat payment and clearance page actions as real shared-database writes; do not test them against operational records without approval.
- Keep export, download, and financial-aid controls as placeholders until their data contracts are implemented.
- Do not put database writes, payment processing, or hidden API calls in a Razor view.
- Use accessible labels, meaningful empty states, validation messages, and keyboard-operable controls when real forms are introduced.
- Do not expose financial or personally identifiable information in HTML, logs, query strings, or client-side storage.

## Backend editing guide

### Correct locations by backend responsibility

| Responsibility | Location to edit |
|---|---|
| Register a Finance service | `Departments/Finance/FinanceMain/Program.cs` |
| Define request/response DTOs or Finance enums | `Departments/Finance/FinanceMain/Contracts/ServiceContracts.cs` |
| Define input validation | `Departments/Finance/FinanceMain/Contracts/Validators.cs` or a new Finance validator file |
| Implement Finance business logic | `Departments/Finance/FinanceMain/Services/` |
| Define service interfaces | Matching `I*.cs` file in `Departments/Finance/FinanceMain/Services/` |
| Add or change persistence | Use the shared MySQL `DefaultConnection` and coordinate changes to shared/Finance tables; do not create a Finance-only database or SQL Server context |
| Add HTTP API endpoints | A Finance-local controller under `Departments/Finance/FinanceMain/Controllers/` or approved Razor Page handlers |
| Configure JWT, payment, or data-provider settings | `appsettings.json` plus environment-specific configuration; never commit secrets |
| Add security headers | `Departments/Finance/FinanceMain/Services/SecurityHeadersExtensions.cs`, then explicitly register the extension in `Program.cs` if required |

### Backend implementation sequence for real finance features

1. Define the Finance contract and authorization rules.
2. Add validation for amounts, identifiers, dates, status transitions, and idempotency keys.
3. Use shared `user.id` for student references and make schema updates idempotent and compatible with existing `mydb` records.
4. Implement the service interface and transaction/error behavior.
5. Register the service in `Program.cs`.
6. Add an authorized endpoint or page handler and map only the required fields to the UI.
7. Add audit events for payment, invoice, aid, export, and authorization-sensitive operations.
8. Add tests for authorization, validation, retries, duplicate requests, concurrency, and failure paths.

Do not connect a payment provider, create production billing calculations, or add cross-department controller/service calls without an approved contract and security review.

## Function inventory

This inventory reflects functions present in Finance-local code at the review date. “Implemented” means code exists; it does not mean the function is connected to a finance page or production storage.

### Application and validation

| Function | Location | Purpose | Status |
|---|---|---|---|
| `WebApplication.CreateBuilder` setup | `Program.cs` | Creates the ASP.NET Core host and registers controllers, validators, services, and Razor Pages | Implemented |
| `StudentRequestValidator()` | `Contracts/Validators.cs` | Validates subject, details, optional safety text, and urgency | Implemented, guidance-specific |
| `ClaimsPrincipalExtensions.GetStudentId()` | `Contracts/ServiceContracts.cs` | Reads the `StudentId` claim as a `Guid` | Implemented, guidance-specific |

### Authentication and token functions

| Function | Location | Purpose | Status |
|---|---|---|---|
| `AuthService.LoginAsync()` | `Services/AuthService.cs` | Checks non-empty credentials and Student/Counselor role, then issues tokens | Implemented, not wired to an endpoint |
| `AuthService.RefreshAsync()` | `Services/AuthService.cs` | Consumes a refresh token and issues a new token pair | Implemented, in-memory store |
| `AuthService.GetStudentId()` | `Services/AuthService.cs` | Delegates student claim extraction | Implemented |
| `AuthService.IssueAsync()` | `Services/AuthService.cs` | Creates JWT and secure access/refresh cookies | Implemented, private helper |
| `InMemoryRefreshTokenStore.StoreAsync()` | `Services/ServiceDependencies.cs` | Stores a refresh token in process memory | Implemented, non-persistent |
| `InMemoryRefreshTokenStore.ConsumeAsync()` | `Services/ServiceDependencies.cs` | Removes and validates a stored refresh token | Implemented, non-persistent |

### Student-request and triage functions

| Function | Location | Purpose | Status |
|---|---|---|---|
| `StudentRequestService.CreateAsync()` | `Services/StudentRequestService.cs` | Validates and creates an idempotent guidance request | Implemented, guidance-specific |
| `StudentRequestService.GetAsync()` | `Services/StudentRequestService.cs` | Retrieves a request owned by a student | Implemented, guidance-specific |
| `StudentRequestService.UpdateAsync()` | `Services/StudentRequestService.cs` | Validates and updates a request with row-version concurrency checking | Implemented, guidance-specific |
| `StudentRequestService.DeleteAsync()` | `Services/StudentRequestService.cs` | Deletes a request with ownership and row-version checks | Implemented, guidance-specific |
| `StudentRequestService.Sanitize()` | `Services/StudentRequestService.cs` | Trims and HTML-encodes optional text | Implemented, private helper |
| `CounselorTriageService.ListAsync()` | `Services/CounselorTriageService.cs` | Filters guidance requests | Implemented, guidance-specific |
| `CounselorTriageService.TransitionAsync()` | `Services/CounselorTriageService.cs` | Moves a request through allowed states and audits the transition | Implemented, guidance-specific |
| `CounselorTriageService.IsValid()` | `Services/CounselorTriageService.cs` | Allows Requested -> InProgress -> Resolved only | Implemented, private helper |
| `InMemoryGuidanceRequestStore.FindAsync()` | `Services/ServiceDependencies.cs` | Finds a guidance record by ID | Implemented, non-persistent |
| `InMemoryGuidanceRequestStore.FindByIdempotencyKeyAsync()` | `Services/ServiceDependencies.cs` | Finds a student request replay | Implemented, non-persistent |
| `InMemoryGuidanceRequestStore.ListAsync()` | `Services/ServiceDependencies.cs` | Filters stored guidance requests | Implemented, non-persistent |
| `InMemoryGuidanceRequestStore.AddAsync()` | `Services/ServiceDependencies.cs` | Adds a request under a lock | Implemented, non-persistent |
| `InMemoryGuidanceRequestStore.SaveAsync()` | `Services/ServiceDependencies.cs` | Saves with row-version checking | Implemented, non-persistent |
| `InMemoryGuidanceRequestStore.DeleteAsync()` | `Services/ServiceDependencies.cs` | Deletes with ownership and row-version checking | Implemented, non-persistent |

### Import, audit, privacy, and notifications

| Function | Location | Purpose | Status |
|---|---|---|---|
| `CsvImportService.ValidateAsync()` | `Services/CsvImportService.cs` | Validates a CSV roster schema and required StudentId values | Implemented, guidance-specific |
| `CsvImportService.CommitAsync()` | `Services/CsvImportService.cs` | Revalidates CSV and returns the row count; does not persist rows | Implemented, dry-run behavior |
| `AuditLogService.AppendAsync()` | `Services/AuditLogService.cs` | Adds an audit event to an in-memory list | Implemented, non-persistent |
| `AuditLogService.QueryAsync()` | `Services/AuditLogService.cs` | Filters audit events by entity and timestamp | Implemented, non-persistent |
| `NotificationService.SendAsync()` | `Services/NotificationService.cs` | Retries outbound delivery and audits failures | Implemented, transport unavailable by default |
| `PiiMaskingService.RedactAsync()` | `Services/PiiMaskingService.cs` | Redacts emails, phone numbers, and SSN-like values | Implemented |
| `UnavailableOutboundMessageTransport.SendAsync()` | `Services/ServiceDependencies.cs` | Fails because no outbound transport is configured | Intentional placeholder |

### Finance functions currently missing

The following Finance behavior is still incomplete or mock-backed:

- invoice records or line items;
- account balances;
- production payment authorization, capture, refund, or reconciliation;
- PDF invoice or statement generation;
- scholarship, award, disbursement, or remaining-aid data;
- fully MySQL-backed assessment, invoice, and aid data.

Assessment, invoice, and aid values still come from demo/in-memory state in some pages. Payment/clearance operations can write shared rows but require live-DB verification and strict student identity checks.

## Configuration and dependencies

- `FinanceMain.csproj` targets `net10.0` and references MySqlConnector, Dapper, JWT bearer authentication, FluentValidation, CsvHelper, Polly, and AspNetCoreRateLimit.
- `Program.cs` currently calls `UseAuthorization()` but does not configure authentication middleware or JWT bearer options. Reconcile this before exposing authenticated Finance endpoints.
- `FinanceDataStore` and copied guidance request/token stores are in-memory and reset when the process stops. Finance's schema bootstrap for `student_payments`, `fee_assessments`, and `student_clearance` runs at startup and fails startup if MySQL is unavailable.
- Configure `ConnectionStrings__DefaultConnection` through the process environment or a secret manager. Do not place credentials in committed settings.
- Store secrets such as `Jwt:SigningKey` outside committed JSON files using the supported environment or secret-management mechanism.
- Review `appsettings.json`, `appsettings.Development.json`, and `Properties/launchSettings.json` before changing ports, URLs, or environment behavior.

## Verification checklist

After frontend-only changes:

```powershell
dotnet build .\Departments\Finance\FinanceMain\FinanceMain.csproj
```

For backend changes, also verify:

- authorized and unauthorized requests;
- validation and error response behavior;
- duplicate/idempotent requests;
- row-version/concurrency conflicts;
- audit records and PII redaction;
- restart behavior when persistent data is expected;
- payment and financial-aid data is not exposed to the wrong user.

## Documentation maintenance

Update this file whenever a Finance page, route, service, contract, DTO, model, persistence layer, authorization rule, or external integration changes. Keep the function inventory aligned with the source and remove entries when files are deleted or responsibilities move.
