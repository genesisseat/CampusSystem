---
name: guidance-sql-persistence
description: Migration of Guidance Department from in-memory stores to SQL persistence.
metadata:
  type: project
---

# Context
The Guidance Department currently uses `InMemoryGuidanceRequestStore` and `InMemoryRefreshTokenStore` for data persistence. These are scaffolding and not suitable for the requirements outlined in the "University Guidance System for Multi-Department Operation" proposal. We need to implement durable SQL persistence using the existing `GuidanceDbContext`.

# Plan

## Phase 1: Persistence Layer Implementation
- **Identify entities**: Ensure `GuidanceRequestRecord` and any refresh token entities are properly mapped in `GuidanceDbContext`.
- **Implement SQL Repositories**: Create `SqlGuidanceRequestStore` and `SqlRefreshTokenStore` implementing `IGuidanceRequestStore` and `IRefreshTokenStore` respectively. Use `IDbContextFactory<GuidanceDbContext>` for thread-safe context creation.

## Phase 2: Dependency Registration
- Update `Program.cs` to remove `InMemoryGuidanceRequestStore` and `InMemoryRefreshTokenStore` registrations.
- Register `SqlGuidanceRequestStore` and `SqlRefreshTokenStore` as scoped services.

## Phase 3: Testing & Verification
- Review existing unit tests in `GuidanceDepartmentMain.Tests` to determine if they need to be updated to support the SQL repository layer or if they can remain using mocks/interfaces.
- Verify that the `Students` table and the new request/token tables exist in the `GuidanceDbContext` schema.

# Verification
- Run tests: `dotnet test .\GuidanceDepartmentMain.Tests\GuidanceDepartmentMain.Tests.csproj`
- Manually verify SQL schema using a query tool or EF Core migrations.
- Confirm the application starts and can perform CRUD operations on requests and tokens against the `CampusSystemDb`.
