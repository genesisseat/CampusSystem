---
name: guidance-sql-persistence
description: Historical plan; Guidance persistence has migrated to shared MySQL.
metadata:
  type: project
---

# Guidance Persistence Status

This document's former EF Core/SQL Server migration plan is superseded. Guidance now uses shared MySQL `mydb` through `ConnectionStrings:DefaultConnection`.

## Active persistence

- `MySqlGuidancePersistenceStores` persists Guidance requests and refresh tokens.
- `GuidanceDbService` reads active student identities from shared MySQL `user` and bootstraps Guidance-owned tables.
- Cross-department student references use integer `user.id`.
- Connection credentials come from `ConnectionStrings__DefaultConnection`, never committed settings.

## Remaining work

- Build and run `GuidanceDepartmentMain.Tests` after service changes.
- Keep the EF Core `GuidanceDbContext` and SQL Server migration files inactive; do not apply or re-register them.
- Implement durable audit storage, counselor authorization, and an approved notification transport before treating those workflows as production-ready.

See [`Departments/SHARED-MYSQL-DATABASE.md`](../../SHARED-MYSQL-DATABASE.md) for the campus-wide connection and data contract.
