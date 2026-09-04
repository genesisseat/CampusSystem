[CmdletBinding(SupportsShouldProcess)]
param(
    [string[]] $Department,
    [switch] $SkipRestore
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = $PSScriptRoot
$sourceProject = Join-Path $repositoryRoot 'Departments\GuidanceDepartment\GuidanceDepartmentMain'
$sourceContracts = Join-Path $sourceProject 'Contracts'
$sourceServices = Join-Path $sourceProject 'Services'

$targets = @{
    FacultyPortal = 'Departments\FacultyPortal\FacultyPortalMain'
    Finance = 'Departments\Finance\FinanceMain'
    Library = 'Departments\Library\LibraryMain'
    Registrar = 'Departments\Registrar\RegistrarMain'
    StudentPortal = 'Departments\StudentPortal\StudentPortalMain'
}

if ($Department) {
    $unknown = $Department | Where-Object { -not $targets.ContainsKey($_) }
    if ($unknown) {
        throw "Unknown department(s): $($unknown -join ', '). Valid values: $($targets.Keys -join ', ')"
    }
    $selectedTargets = @{}
    foreach ($selectedDepartment in $Department) {
        $selectedTargets[$selectedDepartment] = $targets[$selectedDepartment]
    }
    $targets = $selectedTargets
}

$packages = @(
    @{ Name = 'Microsoft.AspNetCore.Authentication.JwtBearer'; Version = '10.0.11' }
    @{ Name = 'AspNetCoreRateLimit'; Version = '5.0.0' }
    @{ Name = 'CsvHelper'; Version = '33.1.0' }
    @{ Name = 'FluentValidation.AspNetCore'; Version = '11.3.1' }
    @{ Name = 'Polly'; Version = '8.6.4' }
    @{ Name = 'Microsoft.EntityFrameworkCore'; Version = '10.0.11' }
)

function Invoke-Checked {
    param(
        [string] $FilePath,
        [string[]] $ArgumentList
    )

    & $FilePath @ArgumentList
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($ArgumentList -join ' ')"
    }
}

function Copy-SourceTree {
    param(
        [string] $Source,
        [string] $Destination,
        [string] $SourceNamespace,
        [string] $TargetNamespace,
        [string] $BackupRoot
    )

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    Get-ChildItem -Path $Source -Filter '*.cs' -File | ForEach-Object {
        $destinationFile = Join-Path $Destination $_.Name
        if (Test-Path $destinationFile) {
            $backupFile = Join-Path $BackupRoot (Join-Path (Split-Path $Destination -Leaf) $_.Name)
            New-Item -ItemType Directory -Path (Split-Path $backupFile) -Force | Out-Null
            Copy-Item $destinationFile $backupFile -Force
        }

        $content = Get-Content -Path $_.FullName -Raw
        $content = $content.Replace($SourceNamespace, $TargetNamespace)
        Set-Content -Path $destinationFile -Value $content -Encoding utf8
    }
}

foreach ($entry in $targets.GetEnumerator()) {
    $departmentName = $entry.Key
    $projectDirectory = Join-Path $repositoryRoot $entry.Value
    $projectFile = Join-Path $projectDirectory "$departmentName`Main.csproj"
    $programFile = Join-Path $projectDirectory 'Program.cs'

    if (-not (Test-Path $projectFile) -or -not (Test-Path $programFile)) {
        throw "Could not find expected project files for $departmentName under $projectDirectory."
    }

    $targetNamespace = "$departmentName`Main"
    $backupRoot = Join-Path $projectDirectory '.guidance-services-backup'

    if ($PSCmdlet.ShouldProcess($departmentName, 'Install Guidance services')) {
        New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
        Copy-Item $programFile (Join-Path $backupRoot 'Program.cs') -Force

        Copy-SourceTree -Source $sourceContracts -Destination (Join-Path $projectDirectory 'Contracts') -SourceNamespace 'GuidanceDepartmentMain' -TargetNamespace $targetNamespace -BackupRoot $backupRoot
        Copy-SourceTree -Source $sourceServices -Destination (Join-Path $projectDirectory 'Services') -SourceNamespace 'GuidanceDepartmentMain' -TargetNamespace $targetNamespace -BackupRoot $backupRoot

        $program = Get-Content -Path $programFile -Raw
        if ($program -notmatch '(?m)^using FluentValidation;') {
            $program = "using FluentValidation;`r`n$program"
        }
        if ($program -notmatch "using $targetNamespace\.Contracts;") {
            $program = "using $targetNamespace.Contracts;`r`nusing $targetNamespace.Services;`r`n`r`n$program"
        }

        $registrationMarker = "builder.Services.AddSingleton<IGuidanceRequestStore, InMemoryGuidanceRequestStore>();"
        if ($program -notmatch [regex]::Escape($registrationMarker)) {
            $registrations = @"

// Guidance services
builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssemblyContaining<StudentRequestValidator>();
builder.Services.AddSingleton<IGuidanceRequestStore, InMemoryGuidanceRequestStore>();
builder.Services.AddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();
builder.Services.AddSingleton<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStudentRequestService, StudentRequestService>();
builder.Services.AddScoped<ICounselorTriageService, CounselorTriageService>();
builder.Services.AddScoped<ICsvImportService, CsvImportService>();
builder.Services.AddSingleton<IPiiMaskingService, PiiMaskingService>();
builder.Services.AddScoped<IOutboundMessageTransport, UnavailableOutboundMessageTransport>();
builder.Services.AddScoped<INotificationService, NotificationService>();
"@
            $program = $program.Replace('// Add services to the container.', "// Add services to the container.$registrations")
        }
        Set-Content -Path $programFile -Value $program -Encoding utf8

        foreach ($package in $packages) {
            $existing = Select-String -Path $projectFile -Pattern $package.Name -SimpleMatch
            if (-not $existing) {
                Invoke-Checked 'dotnet' @('add', $projectFile, 'package', $package.Name, '--version', $package.Version)
            }
        }

        if (-not $SkipRestore) {
            Invoke-Checked 'dotnet' @('restore', $projectFile)
        }

        Write-Host "Installed Guidance services in $departmentName. Backup: $backupRoot"
    }
}

Write-Host 'Installation complete.'
