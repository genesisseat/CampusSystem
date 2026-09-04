[CmdletBinding()]
param([switch] $Build)

$projectDirectory = Split-Path $PSScriptRoot -Parent
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$checker = Join-Path $repositoryRoot 'Check-GuidanceServices.ps1'

if (-not (Test-Path -LiteralPath $checker)) {
    throw "Central checker not found: $checker"
}

& $checker -ProjectPath $projectDirectory -Build:$Build
exit $LASTEXITCODE
