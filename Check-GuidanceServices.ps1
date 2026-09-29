[CmdletBinding()]
param(
    [string] $ProjectPath = (Get-Location).Path,
    [switch] $AllDepartments,
    [switch] $Build,
    [switch] $RequireDatabaseConfiguration
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
$connectorFiles = @('CampusSystemDbConnector.cs')
$registrationMarkers = @(
    'IGuidanceRequestStore, SqlGuidanceRequestStore'
    'IRefreshTokenStore, SqlRefreshTokenStore'
    'IAuditLogService, AuditLogService'
    'IAuthService, AuthService'
    'IStudentRequestService, StudentRequestService'
    'ICounselorTriageService, CounselorTriageService'
    'ICsvImportService, CsvImportService'
    'IPiiMaskingService, PiiMaskingService'
    'IOutboundMessageTransport, UnavailableOutboundMessageTransport'
    'INotificationService, NotificationService'
    'CampusSystemDbConnector'
    'CampusSystemDbConnector.Resolve'
    'ConnectionStringName'
)
$packageNames = @('MySqlConnector')

$departmentPaths = [ordered]@{
    FacultyPortal = 'Departments\FacultyPortal\FacultyPortalMain'
    Finance = 'Departments\Finance\FinanceMain'
    GuidanceDepartment = 'Departments\GuidanceDepartment\GuidanceDepartmentMain'
    Library = 'Departments\Library\LibraryMain'
    Registrar = 'Departments\Registrar\RegistrarMain'
    StudentPortal = 'Departments\StudentPortal\StudentPortalMain'
    Testing = 'Departments\Testing\TestingMain'
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
    param(
        [string] $Directory,
        [switch] $RequireDatabaseConfiguration
    )

    $projectFile = Get-ChildItem -LiteralPath $Directory -Filter '*.csproj' -File | Select-Object -First 1
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectFile.Name)
    $programFile = Join-Path $Directory 'Program.cs'
    $contractsDirectory = Join-Path $Directory 'Contracts'
    $servicesDirectory = Join-Path $Directory 'Services'
    $connectorFile = Join-Path $Directory '..\..\..\SQL\CampusSystemDbConnector.cs'
    $results = [System.Collections.Generic.List[object]]::new()

    function Add-Result {
        param([string] $Check, [bool] $Passed, [string] $Detail)
        $results.Add([pscustomobject]@{
                Status = if ($Passed) { 'PASS' } else { 'FAIL' }
                Check = $Check
                Detail = $Detail
            })
    }

    $program = if (Test-Path -LiteralPath $programFile) { Get-Content -LiteralPath $programFile -Raw } else { '' }
    Add-Result 'Program.cs' (Test-Path -LiteralPath $programFile) $(if ($program) { 'Present' } else { 'Missing' })

    $appSettingsPath = Join-Path $Directory 'appsettings.json'
    $appSettings = if (Test-Path -LiteralPath $appSettingsPath) { Get-Content -LiteralPath $appSettingsPath -Raw } else { '' }
    try {
        $null = $appSettings | ConvertFrom-Json -ErrorAction Stop
        Add-Result 'appsettings.json' $true 'Valid JSON'
    }
    catch {
        Add-Result 'appsettings.json' $false 'Invalid JSON'
    }

    $embeddedCredentials = $appSettings -match 'Pwd=|Password='
    Add-Result 'No checked-in DB credentials' (-not $embeddedCredentials) $(if ($embeddedCredentials) { 'Credentials found in appsettings.json' } else { 'No credentials in appsettings.json' })
    $legacyConnection = $appSettings -match 'CampusSystemDb|localhost,1433'
    Add-Result 'No legacy SQL Server connection' (-not $legacyConnection) $(if ($legacyConnection) { 'Legacy connection remains in appsettings.json' } else { 'No legacy connection setting' })

    $projectContents = Get-Content -LiteralPath $projectFile.FullName -Raw
    $sourceFiles = Get-ChildItem -LiteralPath $Directory -Recurse -Filter '*.cs' -File |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
    $sourceText = ($sourceFiles | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n"
    $usesDefaultConnection = $sourceText -match 'GetConnectionString\("DefaultConnection"\)'
    Add-Result 'Shared DefaultConnection usage' $usesDefaultConnection $(if ($usesDefaultConnection) { 'Connection key referenced by application code' } else { 'DefaultConnection is not referenced' })

    foreach ($package in $packageNames) {
        $present = $projectContents.Contains(('Include="{0}"' -f $package))
        Add-Result "Package $package" $present $(if ($present) { 'Referenced' } else { 'Missing reference' })
    }

    if ($sourceText -match '\busing Dapper;') {
        $hasDapper = $projectContents.Contains('Include="Dapper"')
        Add-Result 'Package Dapper' $hasDapper $(if ($hasDapper) { 'Referenced' } else { 'Missing reference' })
    }

    $usesSqlServer = $program -match 'UseSqlServer|CampusSystemDbConnector'
    Add-Result 'No SQL Server runtime registration' (-not $usesSqlServer) $(if ($usesSqlServer) { 'SQL Server registration remains in Program.cs' } else { 'No SQL Server runtime registration' })

    $connectionAvailable = -not [string]::IsNullOrWhiteSpace($env:ConnectionStrings__DefaultConnection)
    $connectionCheckPassed = $connectionAvailable -or -not $RequireDatabaseConfiguration
    $connectionDetail = if ($connectionAvailable) {
        'Connection variable is set for this process'
    } elseif ($RequireDatabaseConfiguration) {
        'Set ConnectionStrings__DefaultConnection before launching the app'
    } else {
        'Not set in this shell; runtime readiness was not checked'
    }
    Add-Result 'MySQL connection environment variable' $connectionCheckPassed $connectionDetail

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
    Test-Project $directory -RequireDatabaseConfiguration:$RequireDatabaseConfiguration
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
