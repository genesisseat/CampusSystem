# Guidance Department Setup Guide

This guide helps a developer or AI agent install, build, test, run, and extend the Guidance Department service.

## Project location

```text
X:\CampusSystem\Departments\GuidanceDepartment\GuidanceDepartmentMain
```

The solution contains:

- `GuidanceDepartmentMain`: ASP.NET Core Web API and static browser menu.
- `GuidanceDepartmentMain.Tests`: xUnit unit tests.

## Prerequisites

Install the following on the development machine:

1. Windows 10 or later.
2. .NET 10 SDK, including the `net10.0` runtime.
3. Git, if the repository is being cloned.
4. An editor such as Visual Studio, Visual Studio Code, or another .NET-compatible IDE.

Verify the SDK:

```powershell
dotnet --version
dotnet --list-sdks
```

The project currently targets `.NET 10`:

```xml
<TargetFramework>net10.0</TargetFramework>
```

## Restore dependencies

From `X:\CampusSystem\Departments\GuidanceDepartment`:

```powershell
dotnet restore .\GuidanceDepartmentMain\GuidanceDepartmentMain.slnx
```

Or from the project directory:

```powershell
cd X:\CampusSystem\Departments\GuidanceDepartment\GuidanceDepartmentMain
dotnet restore .\GuidanceDepartmentMain.slnx
```

NuGet packages are restored from the configured NuGet sources. The main project declares these direct dependencies:

| Package | Version | Purpose |
| --- | ---: | --- |
| `Microsoft.AspNetCore.OpenApi` | `10.0.11` | OpenAPI document generation |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | `10.0.11` | JWT authentication |
| `AspNetCoreRateLimit` | `5.0.0` | Request rate limiting |
| `CsvHelper` | `33.1.0` | CSV roster import and validation |
| `FluentValidation.AspNetCore` | `11.3.1` | DTO validation integration |
| `Polly` | `8.6.4` | Notification retry and circuit breaker |
| `Microsoft.EntityFrameworkCore` | `10.0.11` | Persistence abstraction and concurrency types |

The test project additionally uses:

- `Microsoft.NET.Test.Sdk` `18.0.0`
- `xunit` `2.9.3`
- `xunit.runner.visualstudio` `3.1.4`
- `Moq` `4.20.72`

Do not install these packages globally. Keep dependency changes in the relevant `.csproj` file and run restore afterward.

## Build

Build the complete solution:

```powershell
cd X:\CampusSystem\Departments\GuidanceDepartment\GuidanceDepartmentMain
dotnet build .\GuidanceDepartmentMain.slnx
```

Build only the web project:

```powershell
dotnet build .\GuidanceDepartmentMain.csproj
```

If a previous development server is running, stop it with `Ctrl+C` before rebuilding. A running `GuidanceDepartmentMain.exe` can lock files in `bin\Debug\net10.0` and cause a copy or delete error.

To validate without creating or replacing the Windows apphost executable:

```powershell
dotnet build .\GuidanceDepartmentMain.csproj -p:UseAppHost=false
```

## Test

Run all tests:

```powershell
cd X:\CampusSystem\Departments\GuidanceDepartment\GuidanceDepartmentMain
dotnet test .\GuidanceDepartmentMain.Tests\GuidanceDepartmentMain.Tests.csproj
```

Run tests without rebuilding:

```powershell
dotnet test .\GuidanceDepartmentMain.Tests\GuidanceDepartmentMain.Tests.csproj --no-build
```

The tests cover request ownership and idempotency, counselor concurrency conflicts, CSV validation, and notification retry behavior.

## Configuration and secrets

`appsettings.json` contains non-secret defaults and rate-limit rules. Do not put JWT signing keys, passwords, connection strings, or provider credentials in committed JSON files.

For local development, initialize user secrets once:

```powershell
dotnet user-secrets init --project .\GuidanceDepartmentMain.csproj
dotnet user-secrets set "Jwt:SigningKey" "replace-with-a-long-development-only-secret" --project .\GuidanceDepartmentMain.csproj
```

The application reads the JWT key from `Jwt:SigningKey`. Use environment variables, a managed secret store, or Azure Key Vault for deployed environments.

## Run locally

From `X:\CampusSystem\Departments\GuidanceDepartment`:

```powershell
dotnet run --project .\GuidanceDepartmentMain\GuidanceDepartmentMain.csproj --launch-profile http
```

From `X:\CampusSystem\Departments\GuidanceDepartment\GuidanceDepartmentMain`:

```powershell
dotnet run --launch-profile http
```

Open the browser interface at:

```text
http://localhost:5149/
```

The development OpenAPI document is available at:

```text
http://localhost:5149/openapi/v1.json
```

The HTTPS profile uses `https://localhost:7130` and `http://localhost:5149`. If the HTTPS development certificate is not trusted, run:

```powershell
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

Do not use the clean command on a machine where other local .NET applications depend on an existing development certificate without checking first.

## Service and dependency registration

Application services are registered in `Program.cs`. When adding a service:

1. Add the interface and implementation under `Services`.
2. Register the implementation with the correct lifetime in `Program.cs`.
3. Add or update focused tests.
4. Build and run the test project.

The current notification registration is:

```csharp
builder.Services.AddScoped<IOutboundMessageTransport, UnavailableOutboundMessageTransport>();
builder.Services.AddScoped<INotificationService, NotificationService>();
```

`UnavailableOutboundMessageTransport` is a development fallback. It throws a clear configuration error, allowing `NotificationService` to retry and audit the failed delivery. Replace it with a real email, SMS, or messaging transport before production.

## Current development limitations

The following are scaffolding implementations and are not production persistence:

- `InMemoryGuidanceRequestStore`
- `InMemoryRefreshTokenStore`
- `AuditLogService`
- `UnavailableOutboundMessageTransport`

Before production deployment, replace them with durable, secured implementations. Add a real database provider and migrations when persistence is introduced.

## Security checks

Run the package vulnerability check before merging dependency changes:

```powershell
dotnet list .\GuidanceDepartmentMain.slnx package --vulnerable
```

Also verify that:

- JWT signing keys are secret and sufficiently long.
- HTTPS is enabled outside local development.
- Authentication and authorization policies are tested.
- Student identity comes from claims, not request payloads.
- PII is masked in logs, exports, and summaries.
- Audit records are durable and access-controlled in production.

## Smart App Control

Windows Smart App Control may block unsigned development assemblies, especially test output or generated apphost files. This is an operating-system policy and cannot be fixed by adding a C# interface or by strong-name signing.

If the error contains `Application Control policy has blocked this file (0x800711C7)`:

1. Review Windows Security > Virus & threat protection > Protection history to identify the blocked file.
2. Confirm the build output belongs to this trusted source tree.
3. Run through the .NET host instead of launching the generated `.exe` directly:

```powershell
dotnet run --project .\GuidanceDepartmentMain\GuidanceDepartmentMain.csproj --launch-profile http
```

4. For organizational or production use, have the responsible administrator sign binaries with a certificate trusted by the device and follow the organization’s application-control policy.

Do not disable Smart App Control or create policy exceptions unless you administer the machine and have verified the entire build chain.

## AI agent workflow

When modifying this project, an AI agent should:

1. Read this guide and `SERVICES.md`.
2. Inspect the owning service, interface, registration, and nearby tests before editing.
3. Preserve existing public APIs and service lifetimes unless the change requires otherwise.
4. Keep secrets out of source control.
5. Run a focused build or test immediately after the first edit.
6. Run the full relevant test project before reporting completion.
7. Mention any machine policy, locked process, missing SDK, or unrelated test failure explicitly.
