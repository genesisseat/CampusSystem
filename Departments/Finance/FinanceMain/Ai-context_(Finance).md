# Finance AI Context

## Ownership

Finance owns billing, payment, and financial-aid interfaces in this project. Keep changes local to `FinanceMain` unless a shared contract or platform change is required.

## How to edit this project

Use the task type to decide the correct layer:

- Front-end / interface work: edit only Razor Pages in `Pages/`, the shared layout in `Pages/Shared/_Layout.cshtml`, and styling in `wwwroot/css/site.css`. Keep forms and actions presentation-only until a real contract and approval are in place.
- Back-end / data work: edit `Controllers/`, `Services/`, `Contracts/`, models, DTOs, `Program.cs`, and other server-side files only when the task explicitly requires payment, invoice, or aid integration.
- AI model rule: if the request does not clearly ask for backend logic, assume it is a UI edit and do not add persistence, payment processing, or external API calls.

## AI maintenance manual

This file is the project guide for future AI sessions and developer handoffs. It should stay current as the project changes.

### When to update this file

Update this context whenever any of the following changes:

- a new Razor Page, folder, or route is added under `Pages/`
- a new controller, service, contract, model, or DTO is created
- a page starts depending on real billing, payment, or aid data
- department ownership or security responsibilities change
- a page moves from mock UI to real backend behavior

### Required update pattern

When the project structure changes, revise these sections in order:

1. `Ownership` — confirm which team and application area owns the feature
2. `How to edit this project` — confirm whether the work is UI-only or backend
3. `Department UI` — add or remove page names and responsibilities
4. `Files to edit for UI changes` and `What not to add while editing the interface` — keep them aligned with the current files
5. `Change Rules` — add any new auth, validation, or contract requirements

### AI session rule

Before making a change in a future session, read this file first and compare it to the current project structure. If the app has new pages, services, policies, or contract files, update this file to match the real state before continuing the task.

### Project map

- `Pages/` = UI pages and presentation logic
- `Pages/Shared/_Layout.cshtml` = shared shell and global styling entry point
- `wwwroot/css/site.css` = visual theme and component styling
- `Controllers/` = request handling and endpoint behavior
- `Services/` = business logic and integrations
- `Contracts/` = interfaces and shared DTOs
- `Models/` = domain objects and data contracts

## Interface-only editing guidance

Finance uses a service layer and shared MySQL `mydb`, but data maturity varies: assessment and ledger pages still use `FinanceDataStore` demo data, while payment/clearance services can write shared MySQL records. Keep these boundaries explicit when changing a page; do not describe the whole department as either fully mocked or fully synchronized.

### Files to edit for UI changes

- Razor Pages in `Pages/`
- Shared layout in `Pages/Shared/_Layout.cshtml`
- Styling in `wwwroot/css/site.css`
- Supporting front-end assets in `wwwroot/`

### What not to add while editing the interface

- Do not replace demo assessment data with live calculations unless the Finance workflow/schema contract is part of the task.
- Do not assume a payment/clearance action is synchronized until it uses the shared MySQL service and has a verified student `user.id`.
- No hidden API calls that would process real payment data
- Use shared MySQL `mydb` through `ConnectionStrings:DefaultConnection`, supplied through `ConnectionStrings__DefaultConnection` or a secret manager. Shared student references use integer `user.id`; do not create Finance-local student IDs or a second database.
- Direct calls into another department's controllers, services, or API endpoints remain unapproved.

### Approved UI behavior

- Keep payment forms, invoice downloads, and aid actions as display-only placeholders until the contract and security review are complete.
- Preserve the existing Bootstrap structure and page flow.
- Update labels, cards, filters, and styling without introducing live workflow behavior.

## Department UI and persistence status

Finance pages call the service layer, but they are not uniformly backed by shared MySQL. `FinanceDataStore` still supplies demo assessment and ledger data. `ActivePaymentService` can write payments to `student_payments`; clearance writes use `student_clearance` and must resolve the correct shared `user.id`.

- `/Billing`: billing and invoice list
- `/Invoice`: invoice detail, line items, totals, and download placeholder
- `/Payments`: payment history table
- `/Pay`: make-a-payment form; UI only
- `/Aid`: financial aid and scholarship status card

The pages use the existing Bootstrap layout and local styles in `Pages/Shared/_Layout.cshtml` and `wwwroot/css/site.css`. Do not claim that a payment, assessment, aid, or document-fee workflow is synchronized until its service reads/writes the shared MySQL records and has been verified.

## Change Rules

- Do not treat `FinanceDataStore` demo assessments as live student balances.
- Validate a student against `user.id` before any shared payment or clearance write; never fall back to a hard-coded student ID.
- Protect financial and aid information with appropriate access checks before backend wiring.
- Run `dotnet build FinanceMain.csproj` after UI changes.
