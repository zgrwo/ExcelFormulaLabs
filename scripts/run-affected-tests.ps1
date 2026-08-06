#Requires -Version 5.1
<#
.SYNOPSIS
    Run only tests affected by changed source files.
.DESCRIPTION
    Maps changed source files to their corresponding test files and runs
    only the affected test assemblies. Supports:
    - Core → CoreTests mapping (e.g. StatsCore.cs → StatsCoreTests.cs)
    - Udf → UdfTests mapping (e.g. StatsUdf.cs → StatsUdfTests.cs)
    - Foundation → Foundation.Tests project
    - Analytics → Analytics.Tests project
    - DataToolkit → DataToolkit.Tests project
.PARAMETER ChangedFiles
    Array of changed file paths (relative or absolute).
    If omitted, auto-detects from git diff (unstaged + staged).
.PARAMETER DryRun
    Show which tests would run without executing them.
.EXAMPLE
    .\scripts\run-affected-tests.ps1
    # Auto-detect changes from git and run affected tests

.EXAMPLE
    .\scripts\run-affected-tests.ps1 -ChangedFiles src/Analytics/StatsCore.cs
    # Run only StatsCore-related tests

.EXAMPLE
    .\scripts\run-affected-tests.ps1 -DryRun
    # Show what would run without executing
.NOTES
    Usage: .\scripts\run-affected-tests.ps1 [-ChangedFiles <files>] [-DryRun]
#>

param(
    [string[]]$ChangedFiles,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

Write-Host "============================================================"
Write-Host "  Affected Test Router"
Write-Host "============================================================"

# -- Resolve changed files --
if (-not $ChangedFiles -or $ChangedFiles.Count -eq 0) {
    Write-Host ""
    Write-Host "Auto-detecting changes from git ..." -ForegroundColor Cyan
    $gitFiles = git -C $repoRoot diff --name-only HEAD 2>$null
    if (-not $gitFiles) {
        $gitFiles = git -C $repoRoot diff --name-only 2>$null
    }
    if (-not $gitFiles) {
        Write-Host "  No changes detected. Nothing to test." -ForegroundColor Yellow
        exit 0
    }
    $ChangedFiles = @($gitFiles)
    Write-Host "  Found $($ChangedFiles.Count) changed file(s)" -ForegroundColor Cyan
}

# -- Module mapping --
$moduleMap = @{
    "Foundation"    = "Foundation.Tests"
    "Analytics"     = "Analytics.Tests"
    "DataToolkit"   = "DataToolkit.Tests"
}

# -- Map changed files to test projects and filters --
$affectedProjects = @{}
$affectedFilters = @()

foreach ($file in $ChangedFiles) {
    $normalized = $file -replace '\\', '/'

    # Match src/<Module>/<Name>Core.cs or <Name>Udf.cs pattern
    if ($normalized -match '^src/(\w+)/(\w+)(Core|Udf|Helpers|AsyncUdf)\.cs$') {
        $module = $Matches[1]
        $className = $Matches[2]
        $suffix = $Matches[3]

        $testProject = $moduleMap[$module]
        if (-not $testProject) { continue }

        if (-not $affectedProjects.ContainsKey($testProject)) {
            $affectedProjects[$testProject] = @()
        }

        # Map to test class name
        $testClass = switch ($suffix) {
            "Core"      { "${className}CoreTests" }
            "Udf"       { "${className}UdfTests" }
            "Helpers"   { "${className}Tests" }
            "AsyncUdf"  { "${className}Tests" }
            default     { "${className}Tests" }
        }

        $affectedProjects[$testProject] += $testClass
        $affectedFilters += $testClass
        Write-Host "  $normalized -> $testProject/$testClass" -ForegroundColor DarkGray
    }
    # Foundation shared files affect all Foundation tests
    elseif ($normalized -match '^src/Foundation/') {
        if (-not $affectedProjects.ContainsKey("Foundation.Tests")) {
            $affectedProjects["Foundation.Tests"] = @()
        }
        Write-Host "  $normalized -> Foundation.Tests (shared file)" -ForegroundColor DarkGray
    }
    # Test files map directly
    elseif ($normalized -match '^tests/(\w[\w.]+)/') {
        $testProject = $Matches[1]
        if (-not $affectedProjects.ContainsKey($testProject)) {
            $affectedProjects[$testProject] = @()
        }
        Write-Host "  $normalized -> $testProject (test file changed)" -ForegroundColor DarkGray
    }
    # Rules/docs/scripts — no tests affected
    else {
        Write-Host "  $normalized -> (no affected tests)" -ForegroundColor DarkYellow
    }
}

# -- Deduplicate --
$uniqueProjects = $affectedProjects.Keys | Sort-Object
$uniqueFilters = $affectedFilters | Select-Object -Unique

if ($uniqueProjects.Count -eq 0) {
    Write-Host ""
    Write-Host "  No affected test projects. Nothing to run." -ForegroundColor Yellow
    exit 0
}

# -- Summary --
Write-Host ""
Write-Host "Affected test projects:" -ForegroundColor Cyan
foreach ($p in $uniqueProjects) {
    $classes = $affectedProjects[$p] | Select-Object -Unique
    Write-Host "  - $p ($($classes.Count) test class(es))" -ForegroundColor White
}

if ($uniqueFilters.Count -gt 0) {
    Write-Host ""
    Write-Host "Test class filters: $($uniqueFilters -join ', ')" -ForegroundColor Cyan
}

if ($DryRun) {
    Write-Host ""
    Write-Host "  [DRY RUN] No tests executed." -ForegroundColor Yellow
    Write-Host "============================================================"
    exit 0
}

# -- Execute tests --
Write-Host ""
Write-Host "Running affected tests ..." -ForegroundColor Cyan
Write-Host ""

$exitCode = 0
foreach ($project in $uniqueProjects) {
    $testPath = Join-Path $repoRoot "tests/$project"
    if (-not (Test-Path $testPath)) {
        Write-Host "  [SKIP] $testPath not found" -ForegroundColor DarkYellow
        continue
    }

    $classes = $affectedProjects[$project] | Select-Object -Unique
    if ($classes.Count -gt 0) {
        $filterExpr = $classes -join '|'
        Write-Host "  dotnet test tests/$project --filter `"FullyQualifiedName~$filterExpr`"" -ForegroundColor DarkGray
        dotnet test "tests/$project" --filter "FullyQualifiedName~$filterExpr" --no-restore
        if ($LASTEXITCODE -ne 0) { $exitCode = 1 }
    } else {
        Write-Host "  dotnet test tests/$project" -ForegroundColor DarkGray
        dotnet test "tests/$project" --no-restore
        if ($LASTEXITCODE -ne 0) { $exitCode = 1 }
    }
}

Write-Host ""
Write-Host "============================================================"
if ($exitCode -eq 0) {
    Write-Host "  [PASS] All affected tests passed." -ForegroundColor Green
} else {
    Write-Host "  [FAIL] Some affected tests failed." -ForegroundColor Red
}
Write-Host "============================================================"
exit $exitCode
