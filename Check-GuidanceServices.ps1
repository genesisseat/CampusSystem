[CmdletBinding()]
param(
    [string] $ProjectPath = (Get-Location).Path,
    [switch] $AllDepartments,
    [switch] $Build
)

$ErrorActionPreference = 'Stop'

$serviceFiles = @(
    'AuthService.cs'
    'AuditLogService.cs'
    'CounselorTriageService.cs'
    'CsvImportService.cs'
    'IAuthService.cs'
    'IAuditLogService.cs'
    'ICounselorTriageService.cs'
    'ICsvImportService.cs'
    'INotificationService.cs'
    'IPiiMaskingService.cs'
    'IStudentRequestService.cs'
    'NotificationService.cs'
    'PiiMaskingService.cs'
    'SecurityHeadersExtensions.cs'
    'ServiceDependencies.cs'
    'StudentRequestService.cs'
)

$contractFiles = @('ServiceContracts.cs')
$registrationMarkers = @(
    'IGuidanceRequestStore, InMemoryGuidanceRequestStore'
    'IRefreshTokenStore, InMemoryRefreshTokenStore'
    'IAuditLogService, AuditLogService'
    'IAuthService, AuthService'
    'IStudentRequestService, StudentRequestService'
    'ICounselorTriageService, CounselorTriageService'
    'ICsvImportService, CsvImportService'
    'IPiiMaskingService, PiiMaskingService'
    'IOutboundMessageTransport, UnavailableOutboundMessageTransport'
    'INotificationService, NotificationService'
)
$packageNames = @(
    'Microsoft.AspNetCore.Authentication.JwtBearer'
    'AspNetCoreRateLimit'
    'CsvHelper'
    'FluentValidation.AspNetCore'
    'Polly'
)
$efPackageNames = @(
    'Microsoft.EntityFrameworkCore'
    'Microsoft.EntityFrameworkCore.SqlServer'
)

$departmentPaths = [ordered]@{
    FacultyPortal = 'Departments\FacultyPortal\FacultyPortalMain'
    Finance = 'Departments\Finance\FinanceMain'
    GuidanceDepartment = 'Departments\GuidanceDepartment\GuidanceDepartmentMain'
    Library = 'Departments\Library\LibraryMain'
    Registrar = 'Departments\Registrar\RegistrarMain'
    StudentPortal = 'Departments\StudentPortal\StudentPortalMain'
}

function Get-ProjectDirectory {
    param([string] $Path)

    $resolvedPath = (Resolve-Path -LiteralPath $Path).Path
    $projectFiles = @(Get-ChildItem -LiteralPath $resolvedPath -Filter '*.csproj' -File)
    if ($projectFiles.Count -eq 1) {
        return $resolvedPath
    }

    if ($projectFiles.Count -gt 1) {
        throw "Multiple project files found in $resolvedPath. Pass the specific project folder."
    }

    throw "No .csproj file found in $resolvedPath."
}

function Test-Project {
    param([string] $Directory)

    $projectFile = Get-ChildItem -LiteralPath $Directory -Filter '*.csproj' -File | Select-Object -First 1
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectFile.Name)
    $programFile = Join-Path $Directory 'Program.cs'
    $contractsDirectory = Join-Path $Directory 'Contracts'
    $servicesDirectory = Join-Path $Directory 'Services'
    $results = [System.Collections.Generic.List[object]]::new()

    function Add-Result {
        param([string] $Check, [bool] $Passed, [string] $Detail)
        $results.Add([pscustomobject]@{
                Status = if ($Passed) { 'PASS' } else { 'FAIL' }
                Check = $Check
                Detail = $Detail
            })
    }

    foreach ($file in $contractFiles) {
        $path = Join-Path $contractsDirectory $file
        Add-Result "Contract $file" (Test-Path -LiteralPath $path) $(if (Test-Path -LiteralPath $path) { 'Present' } else { 'Missing' })
    }

    foreach ($file in $serviceFiles) {
        $path = Join-Path $servicesDirectory $file
        Add-Result "Service $file" (Test-Path -LiteralPath $path) $(if (Test-Path -LiteralPath $path) { 'Present' } else { 'Missing' })
    }

    $program = if (Test-Path -LiteralPath $programFile) { Get-Content -LiteralPath $programFile -Raw } else { '' }
    Add-Result 'Program.cs' (Test-Path -LiteralPath $programFile) $(if ($program) { 'Present' } else { 'Missing' })

    $expectedUsings = @("using $projectName.Contracts;", "using $projectName.Services;")
    foreach ($using in $expectedUsings) {
        Add-Result $using ($program.Contains($using)) $(if ($program.Contains($using)) { 'Present' } else { 'Missing' })
    }

    foreach ($marker in $registrationMarkers) {
        $present = $program.Contains($marker)
        Add-Result "DI $marker" $present $(if ($present) { 'Registered' } else { 'Missing registration' })
    }

    $projectContents = Get-Content -LiteralPath $projectFile.FullName -Raw
    foreach ($package in $packageNames) {
        $present = $projectContents.Contains(('Include="{0}"' -f $package))
        Add-Result "Package $package" $present $(if ($present) { 'Referenced' } else { 'Missing reference' })
    }

    $efPackage = @($efPackageNames | Where-Object { $projectContents.Contains(('Include="{0}"' -f $_)) })
    $efPackagePresent = $efPackage.Count -gt 0
    Add-Result 'Package Microsoft.EntityFrameworkCore or provider' $efPackagePresent $(if ($efPackagePresent) { "Referenced: $($efPackage -join ', ')" } else { 'Missing EF Core package or provider' })

    if ($Build) {
        $buildOutput = & dotnet build $projectFile.FullName --no-restore --nologo 2>&1
        $buildPassed = $LASTEXITCODE -eq 0
        Add-Result 'dotnet build' $buildPassed $(if ($buildPassed) { 'Build succeeded' } else { "Build failed with exit code $LASTEXITCODE" })
    }

    $failed = @($results | Where-Object { $_.Status -eq 'FAIL' })
    [pscustomobject]@{
        Project = $projectName
        Directory = $Directory
        Passed = $failed.Count -eq 0
        Results = $results
        Missing = @($failed | Select-Object -ExpandProperty Check)
    }
}

$directories = @()
if ($AllDepartments) {
    $repositoryRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
    foreach ($relativePath in $departmentPaths.Values) {
        $candidate = Join-Path $repositoryRoot $relativePath
        if (Test-Path -LiteralPath $candidate) {
            $directories += Get-ProjectDirectory $candidate
        }
    }
} else {
    $directories = @(Get-ProjectDirectory $ProjectPath)
}

$reports = foreach ($directory in $directories) {
    Test-Project $directory
}

foreach ($report in $reports) {
    $status = if ($report.Passed) { 'HEALTHY' } else { 'MISSING ITEMS' }
    Write-Host "`n$($report.Project): $status"
    $report.Results | Format-Table -AutoSize | Out-Host
    if (-not $report.Passed) {
        Write-Host "Missing: $($report.Missing -join '; ')" -ForegroundColor Yellow
    }
}

$allReports = @($reports)
$unhealthy = @($allReports | Where-Object { -not $_.Passed })
$healthyCount = @($allReports | Where-Object Passed).Count
Write-Host "`nChecked $($allReports.Count) project(s): $healthyCount healthy, $($unhealthy.Count) with missing items."
if ($unhealthy.Count -gt 0) {
    exit 1
}
