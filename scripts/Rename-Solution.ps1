<#
.SYNOPSIS
  Safely renames this .NET solution, projects, folders, namespaces, and tooling paths.

.DESCRIPTION
  Dotnet renames often break because ProjectReference paths, namespaces, Docker/CI
  paths, and stale bin/obj get out of sync. This script does a full rename in a
  deterministic order and verifies with restore + build.

.PARAMETER NewName
  New solution/root name (PascalCase identifier), e.g. MyCrm, AcmeErp, HrPlatform.

.PARAMETER OldName
  Current name. Auto-detected from the *.sln file when omitted.

.PARAMETER SkipBuild
  Skip final restore/build verification.

.EXAMPLE
  .\scripts\Rename-Solution.ps1 -NewName MyCrm

.EXAMPLE
  .\scripts\Rename-Solution.ps1 -OldName BaseWebApi -NewName AcmeErp
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z_][A-Za-z0-9_]*$')]
    [string]$NewName,

    [Parameter(Mandatory = $false)]
    [ValidatePattern('^[A-Za-z_][A-Za-z0-9_]*$')]
    [string]$OldName,

    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-RepoRoot {
    $here = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($here)) {
        $here = (Get-Location).Path
    }
    # scripts/ -> repo root
    $root = Split-Path -Parent $here
    if (-not (Test-Path (Join-Path $root '*.sln'))) {
        $root = (Get-Location).Path
    }
    return (Resolve-Path $root).Path
}

function Assert-Identifier([string]$name, [string]$label) {
    if ($name -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') {
        throw "$label '$name' is invalid. Use a C# identifier (letters/digits/underscore, no spaces/dots)."
    }
    if ($name -match '\.') {
        throw "$label must be the ROOT name only (e.g. MyCrm), not MyCrm.Application."
    }
}

function Get-TextFiles([string]$root) {
    $extensions = @(
        '.cs', '.csproj', '.sln', '.json', '.yml', '.yaml', '.md', '.proto',
        '.props', '.targets', '.http', '.xml', '.config', '.txt', '.ps1', '.sh'
    )
    $names = @('Dockerfile', 'docker-compose.yml', '.gitignore', '.dockerignore', '.gitlab-ci.yml')

    Get-ChildItem -Path $root -Recurse -File -Force -ErrorAction SilentlyContinue |
        Where-Object {
            $rel = $_.FullName
            if ($rel -match '[\\/](\.git|bin|obj|\.vs|\.idea|TestResults|publish|\.nuget)[\\/]') {
                return $false
            }
            # Keep this script stable during rename (examples use the current name).
            if ($_.Name -eq 'Rename-Solution.ps1') {
                return $false
            }
            return ($extensions -contains $_.Extension.ToLowerInvariant()) -or ($names -contains $_.Name)
        }
}

function Replace-InFile([string]$path, [hashtable]$map) {
    $content = [System.IO.File]::ReadAllText($path)
    $original = $content
    foreach ($key in ($map.Keys | Sort-Object { $_.Length } -Descending)) {
        $content = $content.Replace($key, [string]$map[$key])
    }
    if ($content -ne $original) {
        $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
        [System.IO.File]::WriteAllText($path, $content, $utf8NoBom)
        return $true
    }
    return $false
}

$root = Get-RepoRoot
Set-Location $root
Write-Host "Repo root: $root"

$sln = Get-ChildItem -Path $root -Filter '*.sln' -File | Select-Object -First 1
if (-not $sln) {
    throw "No .sln found in $root"
}

if ([string]::IsNullOrWhiteSpace($OldName)) {
    $OldName = [System.IO.Path]::GetFileNameWithoutExtension($sln.Name)
}

Assert-Identifier $OldName 'OldName'
Assert-Identifier $NewName 'NewName'

if ($OldName -eq $NewName) {
    Write-Host "OldName and NewName are the same ('$NewName'). Nothing to do."
    exit 0
}

$layers = @('Shared', 'Domain', 'Application', 'Infrastructure', 'Api')
foreach ($layer in $layers) {
    $oldDir = Join-Path $root "$OldName.$layer"
    if (-not (Test-Path $oldDir)) {
        throw "Expected project folder missing: $OldName.$layer"
    }
}

$oldLower = $OldName.ToLowerInvariant()
$newLower = $NewName.ToLowerInvariant()

Write-Host ""
Write-Host "Rename plan"
Write-Host "  Solution : $OldName.sln  ->  $NewName.sln"
Write-Host "  Projects : $OldName.Shared / Domain / Application / Infrastructure / Api"
Write-Host "          -> $NewName.Shared / Domain / Application / Infrastructure / Api"
Write-Host "  Namespaces / Docker / CI text: '$OldName' -> '$NewName'"
Write-Host "  Lowercase tokens: '$oldLower' -> '$newLower'"
Write-Host ""
if (-not $PSCmdlet.ShouldProcess("$OldName -> $NewName", 'Rename solution')) {
    exit 0
}

# -----------------------------------------------------------------------------
# 1) Close stale build outputs (prevents locked/partial rename failures)
# -----------------------------------------------------------------------------
Write-Host "`n[1/7] Cleaning bin/obj/.vs ..."
Get-ChildItem -Path $root -Recurse -Directory -Force |
    Where-Object { $_.Name -in @('bin', 'obj', '.vs') } |
    ForEach-Object {
        Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
    }

# -----------------------------------------------------------------------------
# 2) Replace text in files BEFORE moving folders (keeps diffs coherent)
# -----------------------------------------------------------------------------
Write-Host "[2/7] Updating file contents ..."
$replacements = [ordered]@{
    $OldName = $NewName
    $oldLower = $newLower
}

# Also cover common hyphenated docker tokens derived from PascalCase
# BaseWebApi -> base-web-api style is NOT auto-derived reliably; we only swap exact tokens.

$updated = 0
foreach ($file in (Get-TextFiles $root)) {
    if (Replace-InFile -path $file.FullName -map $replacements) {
        $updated++
        Write-Host "  updated $($file.FullName.Substring($root.Length + 1))"
    }
}
Write-Host "  $updated file(s) content-updated"

# -----------------------------------------------------------------------------
# 3) Rename .csproj files inside each project folder
# -----------------------------------------------------------------------------
Write-Host "[3/7] Renaming .csproj files ..."
foreach ($layer in $layers) {
    $dir = Join-Path $root "$OldName.$layer"
    $oldProj = Join-Path $dir "$OldName.$layer.csproj"
    $newProj = Join-Path $dir "$NewName.$layer.csproj"
    if (-not (Test-Path $oldProj)) {
        # Content replace may have already rewritten the filename only if someone
        # manually renamed; support both.
        $candidate = Get-ChildItem -Path $dir -Filter '*.csproj' | Select-Object -First 1
        if (-not $candidate) { throw "No .csproj in $dir" }
        $oldProj = $candidate.FullName
    }
    if ((Split-Path -Leaf $oldProj) -ne "$NewName.$layer.csproj") {
        Rename-Item -LiteralPath $oldProj -NewName "$NewName.$layer.csproj"
        Write-Host "  $($OldName).$layer.csproj -> $($NewName).$layer.csproj"
    }
}

# -----------------------------------------------------------------------------
# 4) Rename project directories
# -----------------------------------------------------------------------------
Write-Host "[4/7] Renaming project folders ..."
foreach ($layer in $layers) {
    $oldDir = Join-Path $root "$OldName.$layer"
    $newDir = Join-Path $root "$NewName.$layer"
    if (Test-Path $newDir) {
        throw "Target folder already exists: $NewName.$layer"
    }
    Rename-Item -LiteralPath $oldDir -NewName "$NewName.$layer"
    Write-Host "  $OldName.$layer/ -> $NewName.$layer/"
}

# -----------------------------------------------------------------------------
# 5) Rename solution file
# -----------------------------------------------------------------------------
Write-Host "[5/7] Renaming solution file ..."
$oldSlnPath = Join-Path $root "$OldName.sln"
$newSlnPath = Join-Path $root "$NewName.sln"
if (-not (Test-Path $oldSlnPath)) {
    # Content replace may have changed .sln internal text but not filename
    $oldSlnPath = (Get-ChildItem -Path $root -Filter '*.sln' | Select-Object -First 1).FullName
}
if ((Split-Path -Leaf $oldSlnPath) -ne "$NewName.sln") {
    Rename-Item -LiteralPath $oldSlnPath -NewName "$NewName.sln"
}
$newSlnPath = Join-Path $root "$NewName.sln"
Write-Host "  -> $NewName.sln"

# -----------------------------------------------------------------------------
# 6) Rebuild solution membership (avoids stale project GUIDs/paths)
# -----------------------------------------------------------------------------
Write-Host "[6/7] Recreating solution project entries ..."
# Rewrite a clean solution from templates of existing projects
dotnet new sln -n $NewName -o $root --force | Out-Null
foreach ($layer in $layers) {
    $proj = Join-Path $root "$NewName.$layer\$NewName.$layer.csproj"
    if (-not (Test-Path $proj)) {
        throw "Missing project after rename: $proj"
    }
}
# Add in dependency-friendly order
dotnet sln $newSlnPath add `
    (Join-Path $root "$NewName.Shared\$NewName.Shared.csproj") `
    (Join-Path $root "$NewName.Domain\$NewName.Domain.csproj") `
    (Join-Path $root "$NewName.Application\$NewName.Application.csproj") `
    (Join-Path $root "$NewName.Infrastructure\$NewName.Infrastructure.csproj") `
    (Join-Path $root "$NewName.Api\$NewName.Api.csproj") | Out-Host

# Ensure ProjectReference Include paths use the new folder names
Write-Host "  Verifying ProjectReference paths ..."
Get-ChildItem -Path $root -Recurse -Filter '*.csproj' |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
    ForEach-Object {
        $xml = [System.IO.File]::ReadAllText($_.FullName)
        if ($xml -match [regex]::Escape("..\$OldName.")) {
            throw "Stale ProjectReference still points at '$OldName' in $($_.FullName)"
        }
        # Reject legacy short paths like ..\Domain\ (must be ..\<Name>.Domain\)
        if ($xml -match '\.\.\\(Shared|Domain|Application|Infrastructure|Api)\\') {
            throw "Legacy short ProjectReference path found in $($_.FullName). Expected '..\$NewName.<Layer>\...' form."
        }
    }

# -----------------------------------------------------------------------------
# 7) Restore + build verification
# -----------------------------------------------------------------------------
if ($SkipBuild) {
    Write-Host "[7/7] SkipBuild set - not building."
}
else {
    Write-Host "[7/7] Restoring and building ..."
    dotnet restore $newSlnPath
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }
    dotnet build $newSlnPath -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }
}

Write-Host ""
Write-Host "Rename completed successfully."
Write-Host "  Solution : $NewName.sln"
Write-Host "  Run      : dotnet run --project $NewName.Api/$NewName.Api.csproj --launch-profile https"
Write-Host "  Docker   : docker compose -f $NewName.Api/docker-compose.yml up --build"
Write-Host ""
Write-Host "Tip: reopen the solution in your IDE so it picks up the new paths."
Write-Host ""