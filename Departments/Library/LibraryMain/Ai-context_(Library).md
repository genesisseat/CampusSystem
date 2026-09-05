# Library AI Context

## Ownership

Library owns catalog, circulation, fines, and reservation interfaces in this project. Keep changes local to `LibraryMain` unless a shared contract or platform change is required.

## How to edit this project

Use the task type to decide the correct layer:

- Front-end / interface work: edit only Razor Pages in `Pages/`, the shared layout in `Pages/Shared/_Layout.cshtml`, and styling in `wwwroot/css/site.css`. Adjust labels, cards, tables, and mock state without enabling actual circulation behavior.
- Back-end / data work: edit `Controllers/`, `Services/`, `Contracts/`, models, DTOs, `Program.cs`, and related server files only when the task explicitly requires catalog, checkout, fines, or reservation logic.
- AI model rule: if the request does not clearly ask for backend logic, assume it is a UI edit and do not add database writes, inventory updates, or live service calls.

## AI maintenance manual

This file is the project guide for future AI sessions and developer handoffs. It should stay current as the project changes.

### When to update this file

Update this context whenever any of the following changes:

- a new Razor Page, folder, or route is added under `Pages/`
- a new controller, service, contract, model, or DTO is created
- a page starts depending on real catalog, checkout, fine, or reservation data
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

This project is intended to present library workflows without backend data access. When asked to update the interface, keep the task limited to the front-end presentation layer.

### Files to edit for UI changes

- Razor Pages in `Pages/`
- Shared layout in `Pages/Shared/_Layout.cshtml`
- Styling in `wwwroot/css/site.css`
- Supporting front-end assets in `wwwroot/`

### What not to add while editing the interface

- No catalog checkout logic or inventory writes
- No database-backed fine calculations
- No live reservation persistence or service calls
- No external API calls tied to real circulation data
- A shared campus database (`CampusSystemDb`) exists, along with a shared `Student` identity model in `CampusSystem.Data`. When this department's persistence is built, it gets its own `LibraryDbContext`, its own models, and its own migration history inside this project — following the pattern already used by Registrar. Do not add a `DbSet` for this department's tables into `CampusSystem.Data` or into another department's context.
- Direct calls into another department's controllers, services, or API endpoints remain unapproved.

### Approved UI behavior

- Update search results, badges, and mock tables to match the intended UX without enabling real circulation actions.
- Preserve the existing page structure and Bootstrap styling conventions.
- Keep actions as placeholders until the owning team approves the contract and authorization design.

## Department UI

The following Razor Pages are UI-only presentation placeholders with no database, service, API, or form-submit behavior:

- `/Catalog`: catalog search, filters, and result area
- `/Resource`: resource detail, availability badge, and reserve placeholder
- `/History`: checkout and return history table
- `/Fines`: fines and fees summary
- `/Reservations`: reservation queue list

The pages use the existing Bootstrap layout and local styles in `Pages/Shared/_Layout.cshtml` and `wwwroot/css/site.css`. Keep availability, reservation, and fee values as placeholders until the owning team defines circulation contracts.

## Change Rules

- Do not add persistence or service calls without an approved catalog/circulation contract.
- Add authorization before exposing student circulation or fee records.
- Keep reserve and download controls non-functional until backend behavior is approved.
- Run `dotnet build LibraryMain.csproj` after UI changes.
