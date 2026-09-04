# Health and repair

This folder contains the health check launcher for this department project.

## Health check

From this project directory, run:

```powershell
.\HealthAndRepair\Check-GuidanceServices.ps1
```

The check verifies:

- Required files under `Contracts` and `Services`.
- Project namespace imports.
- Dependency injection registrations in `Program.cs`.
- Required NuGet package references.

To include compilation, run:

```powershell
.\HealthAndRepair\Check-GuidanceServices.ps1 -Build
```

A healthy project ends with `HEALTHY`. Missing items produce `MISSING ITEMS` and the script exits with code 1.

## Repair

GuidanceDepartment is the source project for the shared services. To repair the other departments, run the central installer from the workspace root:

```powershell
cd X:\CampusSystem
.\Install-GuidanceServices.ps1
```

Preview the repair first:

```powershell
.\Install-GuidanceServices.ps1 -WhatIf
```

The installer backs up `Program.cs` and existing service files in `.guidance-services-backup` before replacing them. Review the backup before deleting it. The installed stores and outbound transport are development scaffolding and must be replaced with production implementations before deployment.

## Workspace-wide check

From `X:\CampusSystem`:

```powershell
.\Check-GuidanceServices.ps1 -AllDepartments -Build
```

If scripts are blocked by execution policy, run the command with `-ExecutionPolicy Bypass` through PowerShell. Do not put signing keys or connection strings in source-controlled JSON files.
